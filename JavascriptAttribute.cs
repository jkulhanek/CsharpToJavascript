using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Forcoft.Javascript
{
    /// <summary>
    /// Attribute that inform a javascript expression not to get the value of property,function or field but to use same label
    /// </summary>
    public class JavascriptAttribute:Attribute
    {
        /// <summary>
        /// Type on javascript context
        /// </summary>
        public JavascriptContextType ContextType { get; set; }
        public JavascriptAttribute(JavascriptContextType type)
        {
            this.ContextType = type;
        }
        public JavascriptAttribute()
        {
        }
    }
    /// <summary>
    /// Type of context in which target property is in final Javascript code
    /// </summary>
    public enum JavascriptContextType
    {
        /// <summary>
        /// Defines that function, field or property with JavascriptAttribute and this context will be translated as a global func or property. P.E. getInfo();
        /// </summary>
        Global,
        /// <summary>
        /// Defines that object with JavascriptAttribute and this context will has translation like this in javascript P.E. this.getInfo();
        /// </summary>
        This,
        /// <summary>
        /// Defines that function, field or property with JavascriptAttribute and this context will be translated as this object func or propert. P.E. this.getInfo();
        /// </summary>
        OnThis
    }
}
