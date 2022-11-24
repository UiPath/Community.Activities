namespace UiPath.Shared.Activities.Bindings
{
    /// <summary>
    /// Entry that represents a dependency relationship between 2 properties used for binding.
    /// Taken from System Activities
    /// </summary>
    public class DependantBindingPropertiesModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DependantBindingPropertiesModel"/> class.
        /// </summary>
        /// <param name="dependantProperty"></param>
        /// <param name="property"></param>
        public DependantBindingPropertiesModel(string dependantProperty, string property)
        {
            DependantProperty = dependantProperty;
            Property = property;
        }
        /// <summary>
        /// Property that depends on another.
        /// </summary>
        public string DependantProperty { get; }
    
        /// <summary>
        /// Property that other properties depend on.
        /// </summary>
        public string Property { get; }
    }
}
