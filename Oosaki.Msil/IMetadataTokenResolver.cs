using System;
using System.Reflection;

namespace Oosaki.Msil
{
    public interface IMetadataTokenResolver
    {
        MethodBase ResolveMethod(int methodToken);
        string ResolveString(int token);
        Type ResolveType(int token);
        void MapType(Type fromType, Type toType);
        void MapMethod(MethodBase fromMethod, MethodBase toMethod);
        FieldInfo ResolveField(int token);
        Type ResolveTokenAsType(int token);
        byte[] ResolveSignature(int tokenValue);
    }
}