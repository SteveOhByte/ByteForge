using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace ByteForge.Runtime
{
    /// <summary>
    /// Reads and parses CSV formatted data.
    /// </summary>
    /// <remarks>
    /// CsvReader provides a robust implementation for reading and parsing CSV files.
    /// It supports features like quoted fields, custom delimiters, header validation,
    /// and automatic mapping to typed objects. The reader handles common CSV edge cases
    /// such as escaped quotes, fields containing delimiters, and multi-line fields.
    /// </remarks>
    public class CsvReader : IDisposable
    {
        private readonly TextReader reader;
        private readonly CsvConfiguration configuration;
        private readonly char[] buffer;
        private int bufferPosition;
        private int bufferLength;
        private int currentLineNumber = 1;
        private bool isDisposed;
        private bool hasHeaderRecord = true;
        private string[] fieldHeaders;
        private readonly Dictionary<string, int> headerIndexMap = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Creates a new instance of CsvReader using the specified TextReader and CultureInfo.
        /// </summary>
        /// <param name="reader">The TextReader to use for reading CSV data.</param>
        /// <param name="culture">The CultureInfo to use for reading CSV data.</param>
        /// <remarks>
        /// This constructor uses default configuration settings with the specified culture.
        /// </remarks>
        public CsvReader(TextReader reader, CultureInfo culture)
            : this(reader, new CsvConfiguration(culture))
        {
        }

        /// <summary>
        /// Creates a new instance of CsvReader using the specified TextReader and configuration.
        /// </summary>
        /// <param name="reader">The TextReader to use for reading CSV data.</param>
        /// <param name="configuration">The configuration to use for reading CSV data.</param>
        /// <remarks>
        /// This constructor allows full control over the CSV parsing behaviour through the configuration object.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown if reader or configuration is null.</exception>
        public CsvReader(TextReader reader, CsvConfiguration configuration)
        {
            this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            buffer = new char[this.configuration.BufferSize];
        }

        /// <summary>
        /// Gets all records from the CSV file and converts each to Type T.
        /// </summary>
        /// <typeparam name="T">The Type of records to create.</typeparam>
        /// <returns>An IEnumerable of records.</returns>
        /// <remarks>
        /// This method reads the entire CSV file and converts each row to an instance of T.
        /// It uses reflection to map CSV fields to properties on type T, based on the header names.
        /// Type conversion is performed for each property, with appropriate error handling based on configuration.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">Thrown if the reader has been disposed.</exception>
        /// <exception cref="CsvReaderException">Thrown for various CSV parsing or mapping errors.</exception>
        public IEnumerable<T> GetRecords<T>() where T : new()
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(CsvReader));

            if (hasHeaderRecord && fieldHeaders == null)
                ReadHeader();

            List<T> records = new();
            while (Read())
            {
                T record = new();
                for (int i = 0; i < fieldHeaders.Length; i++)
                {
                    string propertyName = fieldHeaders[i];
                    PropertyInfo propertyInfo = typeof(T).GetProperty(propertyName,
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    if (propertyInfo == null || !propertyInfo.CanWrite) continue;

                    try
                    {
                        object value = GetField(i);

                        // If the field value is empty and the property type isn't string,
                        // Handle it specially
                        if (value is string stringValue && string.IsNullOrEmpty(stringValue) &&
                            propertyInfo.PropertyType != typeof(string))
                        {
                            // Use default value for non-string property types
                            // Default value for value types
                            propertyInfo.SetValue(record,
                                propertyInfo.PropertyType.IsValueType
                                    ? Activator.CreateInstance(propertyInfo.PropertyType)
                                    // null for reference types
                                    : null);
                        }
                        else
                        {
                            // Try to convert the string value to the property type
                            if (propertyInfo.PropertyType == typeof(string))
                            {
                                // For string properties, use the value as is (empty string for empty fields)
                                propertyInfo.SetValue(record, value ?? string.Empty);
                            }
                            else if (value != null)
                            {
                                // Try to convert the value to the property type
                                try
                                {
                                    object convertedValue = Convert.ChangeType(value, propertyInfo.PropertyType,
                                        CultureInfo.InvariantCulture);
                                    propertyInfo.SetValue(record, convertedValue);
                                }
                                catch (Exception ex) when (ex is FormatException or InvalidCastException
                                                               or OverflowException)
                                {
                                    if (configuration.ThrowOnTypeConversionFailure)
                                        throw new CsvReaderException(
                                            $"Failed to convert '{value}' to {propertyInfo.PropertyType.Name} for property '{propertyName}' on line {currentLineNumber}.",
                                            ex);

                                    // Use default value for the property type
                                    propertyInfo.SetValue(record, propertyInfo.PropertyType.IsValueType
                                        ? Activator.CreateInstance(propertyInfo.PropertyType)
                                        : null);
                                }
                            }
                        }
                    }
                    catch (Exception ex) when (ex is not CsvReaderException)
                    {
                        if (configuration.ThrowOnPropertyMappingFailure)
                                throw new CsvReaderException(
                                $"Error mapping property '{propertyName}' on line {currentLineNumber}.", ex);
                    }
                }

                records.Add(record);
            }

            return records;
        }

        /// <summary>
        /// Gets the field at the specified index.
        /// </summary>
        /// <param name="index">The index of the field to get.</param>
        /// <returns>The field value as a string.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the reader has been disposed.</exception>
        /// <exception cref="CsvReaderException">Thrown if Read() hasn't been called or the index is invalid.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the index is outside the bounds of the record.</exception>
        public string GetField(int index)
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(CsvReader));

            if (currentRecord == null)
                throw new CsvReaderException("You must call Read() before calling GetField().");

            if (index < 0 || index >= currentRecord.Length)
                throw new ArgumentOutOfRangeException(nameof(index), "Field index out of range.");

            return currentRecord[index];
        }

        /// <summary>
        /// Gets the field value by the header name.
        /// </summary>
        /// <param name="name">The name of the header to get the field for.</param>
        /// <returns>The field value as a string.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the reader has been disposed.</exception>
        /// <exception cref="CsvReaderException">Thrown if Read() hasn't been called, headers haven't been read, or the header name is not found.</exception>
        public string GetField(string name)
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(CsvReader));

            if (currentRecord == null)
                throw new CsvReaderException("You must call Read() before calling GetField().");

            if (fieldHeaders == null)
                throw new CsvReaderException("Headers have not been read.");

            if (!headerIndexMap.TryGetValue(name, out int index))
                throw new CsvReaderException($"Header '{name}' not found.");

            return GetField(index);
        }

        /// <summary>
        /// Reads the header record.
        /// </summary>
        /// <remarks>
        /// This method reads the first line of the CSV file as the header record,
        /// which contains the names of each field. These names are used to map fields
        /// to properties when reading records. The method also validates headers
        /// according to the configuration settings.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">Thrown if the reader has been disposed.</exception>
        /// <exception cref="CsvReaderException">Thrown if no header record is found, a header is missing, or a duplicate header is found (depending on configuration).</exception>
        public void ReadHeader()
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(CsvReader));

            if (!Read())
                throw new CsvReaderException("No header record was found.");

            fieldHeaders = currentRecord;
            headerIndexMap.Clear();

            for (int i = 0; i < fieldHeaders.Length; i++)
            {
                string header = fieldHeaders[i];
                if (string.IsNullOrWhiteSpace(header) && configuration.ThrowOnHeaderMissing)
                    throw new CsvReaderException($"Header at index {i} is missing or empty.");

                if (!headerIndexMap.ContainsKey(header))
                    headerIndexMap.Add(header, i);
                else if (configuration.ThrowOnDuplicateHeader)
                    throw new CsvReaderException($"Duplicate header '{header}' found.");
            }

            // Clear current record so the header isn't used as a data row
            currentRecord = null;
        }

        /// <summary>
        /// Reads a record from the CSV file.
        /// </summary>
        /// <returns>True if there was a record to read, otherwise false.</returns>
        /// <remarks>
        /// This method reads the next record from the CSV file and makes it available through
        /// the GetField methods. It returns false when there are no more records to read.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">Thrown if the reader has been disposed.</exception>
        /// <exception cref="CsvReaderException">Thrown if an error occurs reading the CSV record.</exception>
        public bool Read()
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(CsvReader));

            try
            {
                currentRecord = ReadRecord();
                if (currentRecord == null)
                    return false;

                currentLineNumber++;
                return true;
            }
            catch (Exception ex) when (ex is not CsvReaderException)
            {
                throw new CsvReaderException($"Error reading CSV record on line {currentLineNumber}.", ex);
            }
        }

        /// <summary>
        /// The current record being processed.
        /// </summary>
        private string[] currentRecord;

        /// <summary>
        /// Reads a single record from the CSV file.
        /// </summary>
        /// <returns>An array of strings representing the fields in the record, or null if at end of file.</returns>
        /// <remarks>
        /// This method handles the low-level parsing of CSV records, including quoted fields,
        /// escaped quotes, and fields containing newlines. It uses a buffered reading approach
        /// for efficiency.
        /// </remarks>
        private string[] ReadRecord()
        {
            List<string> record = new();
            StringBuilder fieldBuilder = new();
            bool inQuotedField = false;
            bool isEof = false;

            while (!isEof)
            {
                if (bufferPosition >= bufferLength)
                {
                    bufferLength = reader.Read(buffer, 0, buffer.Length);
                    bufferPosition = 0;

                    if (bufferLength == 0)
                    {
                        isEof = true;

                        // Add the last field if there's any content
                        if (record.Count > 0 || fieldBuilder.Length > 0)
                            record.Add(fieldBuilder.ToString());
                        else // Empty record at EOF
                            return null;

                        break;
                    }
                }

                char c = buffer[bufferPosition++];

                // Handle line endings
                if (c == '\r')
                {
                    // Check for \r\n
                    if (bufferPosition < bufferLength && buffer[bufferPosition] == '\n')
                        bufferPosition++;

                    if (!inQuotedField)
                    {
                        record.Add(fieldBuilder.ToString());
                        return record.ToArray();
                    }
                    else // Newline in quoted field
                        fieldBuilder.Append(c);
                }
                else if (c == '\n')
                {
                    if (!inQuotedField)
                    {
                        record.Add(fieldBuilder.ToString());
                        return record.ToArray();
                    }
                    else // Newline in quoted field
                        fieldBuilder.Append(c);
                }
                else if (c == configuration.Delimiter)
                {
                    if (!inQuotedField)
                    {
                        record.Add(fieldBuilder.ToString());
                        fieldBuilder.Clear();
                    }
                    else // Delimiter in quoted field
                        fieldBuilder.Append(c);
                }
                else if (c == configuration.Quote)
                {
                    // Check for escaped quotes
                    if (inQuotedField && bufferPosition < bufferLength && buffer[bufferPosition] == configuration.Quote)
                    {
                        // Escaped quote
                        fieldBuilder.Append(c);
                        bufferPosition++;
                    }
                    else // Toggle quoted field state
                        inQuotedField = !inQuotedField;
                }
                else
                    fieldBuilder.Append(c);
            }

            return record.Count > 0 ? record.ToArray() : null;
        }

        /// <summary>
        /// Disposes the CsvReader instance.
        /// </summary>
        /// <remarks>
        /// This method releases all resources used by the CsvReader, including the TextReader.
        /// </remarks>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the CsvReader instance.
        /// </summary>
        /// <param name="disposing">True if called from Dispose(), false if called from finalizer.</param>
        /// <remarks>
        /// This method is called by the Dispose() method and finalizer to release resources.
        /// </remarks>
        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed)
                return;

            if (disposing)
                reader?.Dispose();

            isDisposed = true;
        }
    }

    /// <summary>
    /// Configuration for CsvReader.
    /// </summary>
    /// <remarks>
    /// This class provides configuration options for the CsvReader, including
    /// delimiter settings, culture information, and error handling options.
    /// </remarks>
    public class CsvConfiguration
    {
        /// <summary>
        /// Creates a new CsvConfiguration instance.
        /// </summary>
        /// <param name="culture">The culture to use for reading CSV data.</param>
        /// <exception cref="ArgumentNullException">Thrown if culture is null.</exception>
        /// <remarks>
        /// The culture is used for type conversions when reading CSV data.
        /// </remarks>
        public CsvConfiguration(CultureInfo culture)
        {
            Culture = culture ?? throw new ArgumentNullException(nameof(culture));
        }

        /// <summary>
        /// Gets or sets the culture info used for reading CSV data.
        /// </summary>
        /// <remarks>
        /// The culture is used for type conversions when reading CSV data.
        /// </remarks>
        public CultureInfo Culture { get; set; }

        /// <summary>
        /// Gets or sets the character used to separate fields. Default is comma.
        /// </summary>
        /// <remarks>
        /// This is the character that separates fields in the CSV file.
        /// </remarks>
        public char Delimiter { get; set; } = ',';

        /// <summary>
        /// Gets or sets the character used to quote fields. Default is double quote.
        /// </summary>
        /// <remarks>
        /// This is the character used to enclose fields that contain special characters like delimiters or newlines.
        /// </remarks>
        public char Quote { get; set; } = '"';

        /// <summary>
        /// Gets or sets the size of the buffer used for reading CSV data. Default is 8192.
        /// </summary>
        /// <remarks>
        /// A larger buffer may improve performance but uses more memory.
        /// </remarks>
        public int BufferSize { get; set; } = 8192;

        /// <summary>
        /// Gets or sets a value indicating whether to throw an exception when a header is missing or empty. Default is true.
        /// </summary>
        /// <remarks>
        /// If set to true, missing or empty headers will cause a CsvReaderException.
        /// If set to false, missing or empty headers will be allowed.
        /// </remarks>
        public bool ThrowOnHeaderMissing { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to throw an exception when a duplicate header is found. Default is true.
        /// </summary>
        /// <remarks>
        /// If set to true, duplicate headers will cause a CsvReaderException.
        /// If set to false, only the first occurrence of a header will be used.
        /// </remarks>
        public bool ThrowOnDuplicateHeader { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to throw an exception when type conversion fails. Default is true.
        /// </summary>
        /// <remarks>
        /// If set to true, type conversion failures will cause a CsvReaderException.
        /// If set to false, default values will be used when type conversion fails.
        /// </remarks>
        public bool ThrowOnTypeConversionFailure { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to throw an exception when property mapping fails. Default is true.
        /// </summary>
        /// <remarks>
        /// If set to true, property mapping failures will cause a CsvReaderException.
        /// If set to false, property mapping failures will be ignored.
        /// </remarks>
        public bool ThrowOnPropertyMappingFailure { get; set; } = true;
    }

    /// <summary>
    /// Exception that is thrown when CSV reading fails.
    /// </summary>
    /// <remarks>
    /// This exception provides information about errors that occur during CSV reading,
    /// such as parsing errors, header validation errors, and type conversion errors.
    /// </remarks>
    public class CsvReaderException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the CsvReaderException class.
        /// </summary>
        /// <remarks>
        /// Creates an exception with a default message.
        /// </remarks>
        public CsvReaderException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the CsvReaderException class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <remarks>
        /// Creates an exception with a custom message.
        /// </remarks>
        public CsvReaderException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the CsvReaderException class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="inner">The exception that is the cause of the current exception.</param>
        /// <remarks>
        /// Creates an exception with a custom message and an inner exception.
        /// </remarks>
        public CsvReaderException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}