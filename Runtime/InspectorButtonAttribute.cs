using System;

namespace ByteForge.Runtime
{
    [AttributeUsage(AttributeTargets.Method)]
    public class InspectorButtonAttribute : Attribute
    {
        public string ButtonName { get; }

        public InspectorButtonAttribute(string buttonName = null)
        {
            ButtonName = buttonName;
        }
    }
}