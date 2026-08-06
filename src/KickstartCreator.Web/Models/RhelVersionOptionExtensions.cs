using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace KickstartCreator.Web.Models;

public static class RhelVersionOptionExtensions
{
    public static string GetLabel(this RhelVersionOption value)
    {
        var member = typeof(RhelVersionOption).GetMember(value.ToString())[0];
        var display = member.GetCustomAttribute<DisplayAttribute>();
        return display?.Name ?? value.ToString();
    }
}
