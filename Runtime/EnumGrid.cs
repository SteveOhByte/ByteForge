using System;
using UnityEngine;

namespace ByteForge.Runtime
{
    /// <summary>
    /// A serializable two-dimensional grid that stores enum values.
    /// </summary>
    /// <typeparam name="T1">The enum type to store in the grid.</typeparam>
    /// <remarks>
    /// EnumGrid provides a convenient way to store and access enum values in a 2D grid format.
    /// It handles the mapping between 2D coordinates and the underlying 1D array.
    /// The grid is serializable, making it compatible with Unity's inspector and serialization system.
    /// 
    /// This is particularly useful for level design, state machines, or any scenario where
    /// a grid of categorized values is needed.
    /// 
    /// Example usage:
    /// <code>
    /// public enum TileType { Empty, Wall, Door, Item }
    /// 
    /// [SerializeField]
    /// private EnumGrid&lt;TileType&gt; levelLayout = new EnumGrid&lt;TileType&gt;(10, 10);
    /// 
    /// void SetupLevel()
    /// {
    ///     levelLayout[0, 0] = TileType.Wall;
    ///     TileType tile = levelLayout[5, 3];
    /// }
    /// </code>
    /// </remarks>
    [Serializable]
    public class EnumGrid<T1> where T1 : Enum
    {
        /// <summary>
        /// Gets the number of rows in the grid.
        /// </summary>
        /// <value>The number of rows.</value>
        public int Rows => rows;
        
        /// <summary>
        /// Gets the number of columns in the grid.
        /// </summary>
        /// <value>The number of columns.</value>
        public int Columns => columns;

        /// <summary>
        /// Gets the underlying array containing the grid data.
        /// </summary>
        /// <value>The one-dimensional array representing the grid data.</value>
        /// <remarks>
        /// This property provides direct access to the underlying array.
        /// Note that modifications to this array will affect the grid,
        /// but you must respect the row-major order layout.
        /// </remarks>
        public T1[] Grid => grid;

        /// <summary>
        /// The underlying array storing the grid data in row-major order.
        /// </summary>
        [SerializeField] private T1[] grid;

        /// <summary>
        /// The number of rows in the grid.
        /// </summary>
        private int rows;
        
        /// <summary>
        /// The number of columns in the grid.
        /// </summary>
        private int columns;

        /// <summary>
        /// Creates a new EnumGrid with the specified dimensions.
        /// </summary>
        /// <param name="rows">The number of rows in the grid.</param>
        /// <param name="columns">The number of columns in the grid.</param>
        /// <remarks>
        /// The grid is initialized with default values (typically the first value in the enum).
        /// </remarks>
        public EnumGrid(int rows, int columns)
        {
            this.rows = rows;
            this.columns = columns;
            grid = new T1[rows * columns];
        }

        /// <summary>
        /// Gets or sets the enum value at the specified grid coordinates.
        /// </summary>
        /// <param name="row">The row index (zero-based).</param>
        /// <param name="col">The column index (zero-based).</param>
        /// <returns>The enum value at the specified coordinates.</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// Thrown when the specified coordinates are outside the grid boundaries.
        /// </exception>
        /// <remarks>
        /// This indexer provides a convenient way to access grid cells using [row, column] syntax.
        /// It handles the conversion between 2D coordinates and the 1D storage array.
        /// </remarks>
        public T1 this[int row, int col]
        {
            get
            {
                if (row < 0 || row >= rows || col < 0 || col >= columns)
                    throw new IndexOutOfRangeException();
                return grid[row * columns + col];
            }
            set
            {
                if (row < 0 || row >= rows || col < 0 || col >= columns)
                    throw new IndexOutOfRangeException();
                grid[row * columns + col] = value;
            }
        }
    }
}