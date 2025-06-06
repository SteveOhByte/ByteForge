using System;

namespace ByteForge.Runtime
{
    /// <summary>
    /// Attribute that creates a button in the Inspector to invoke a method.
    /// </summary>
    /// <remarks>
    /// This attribute can be applied to methods in MonoBehaviour classes to create
    /// a button in the Unity Inspector that calls the method when clicked.
    /// This provides a convenient way to expose functionality to designers and testers
    /// without requiring them to run the game or write code.
    /// 
    /// The method can have parameters, which will be displayed as editable fields
    /// in the inspector above the button. Both public and private methods are supported.
    /// 
    /// Example usage:
    /// <code>
    /// // Creates a button labelled "Spawn Enemy"
    /// [InspectorButton("Spawn Enemy")]
    /// private void SpawnEnemy()
    /// {
    ///     // Method implementation
    /// }
    /// 
    /// // Creates a button with a default label (the method name)
    /// [InspectorButton]
    /// private void ResetPlayer()
    /// {
    ///     // Method implementation
    /// }
    /// 
    /// // Creates a button with parameters
    /// [InspectorButton("Fire Projectile")]
    /// private void FireProjectile(float speed, float damage)
    /// {
    ///     // Method implementation using the parameters
    /// }
    /// </code>
    /// 
    /// Note: This attribute must be used in conjunction with a custom editor
    /// or property drawer that implements the button drawing functionality.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method)]
    public class InspectorButtonAttribute : Attribute
    {
        /// <summary>
        /// Gets the custom name for the button.
        /// </summary>
        /// <value>
        /// The custom button name if provided, or null to use the method name.
        /// </value>
        public string ButtonName { get; }

        /// <summary>
        /// Creates a new instance of the InspectorButtonAttribute.
        /// </summary>
        /// <param name="buttonName">
        /// Optional custom name for the button. If null or not provided,
        /// the method name will be used as the button label.
        /// </param>
        /// <remarks>
        /// The button name is displayed on the button in the Inspector.
        /// If you don't provide a name, the method name will be formatted
        /// with spaces between camel case words (e.g., "SpawnEnemy" becomes "Spawn Enemy").
        /// </remarks>
        public InspectorButtonAttribute(string buttonName = null)
        {
            ButtonName = buttonName;
        }
    }
}