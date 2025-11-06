using System;

namespace Cliq.Api.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class SkipApiKeyAttribute : Attribute
    {
    }
}
