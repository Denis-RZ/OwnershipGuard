using Microsoft.AspNetCore.Mvc;

namespace OwnershipGuard;

/// <summary>
/// MVC attribute that enforces ownership (and optional tenant) for an action or controller.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequireOwnershipAttribute : TypeFilterAttribute
{
    /// <param name="routeParamName">Route parameter name containing the resource id (e.g. "id").</param>
    /// <param name="entityType">Entity type registered with <see cref="IOwnershipDescriptorRegistry"/>.</param>
    public RequireOwnershipAttribute(string routeParamName, Type entityType)
        : base(typeof(RequireOwnershipActionFilter))
    {
        Arguments = new object[] { routeParamName, entityType };
    }
}

