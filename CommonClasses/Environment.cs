namespace CustomizationEditor
{
    /// <summary>
    /// Generic class holding environment information
    /// </summary>
    public class Environment
    {
        public string Path { get; set; }
        public string Name { get; set; }

        /// <summary>
        /// Overrides the standard to string for the class normally exposing the type as a string with the name
        /// </summary>
        /// <returns>
        /// Environment.Name current value
        /// </returns>
        public override string ToString()
        {
            return Name;
        }
    }
}
