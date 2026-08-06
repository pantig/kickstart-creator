using System.ComponentModel.DataAnnotations;

namespace KickstartCreator.Web.Models;

/// <summary>
/// Target RHEL 9 minor release. Currently informational only (a label rendered
/// into the generated kickstart's header comment) - every value maps to the
/// same template today via <see cref="Services.IKickstartTemplateProvider"/>.
/// </summary>
public enum RhelVersionOption
{
    [Display(Name = "RHEL 9.4")]
    Rhel94,

    [Display(Name = "RHEL 9.6")]
    Rhel96,

    [Display(Name = "RHEL 9.8")]
    Rhel98,
}
