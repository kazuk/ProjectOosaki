using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Oosaki.Msil.Extentions
{
    public static class TypeExtention
    {
        public static IEnumerable<MemberInfo> GetAllMembers( this Type type)
        {
            return type.GetMembers(BindingFlags.Instance | BindingFlags.Public)
                       .Concat(type.GetMembers(BindingFlags.Instance | BindingFlags.NonPublic))
                       .Concat(type.GetMembers(BindingFlags.Static | BindingFlags.Public))
                       .Concat(type.GetMembers(BindingFlags.Static | BindingFlags.NonPublic));
        }
    }
}
