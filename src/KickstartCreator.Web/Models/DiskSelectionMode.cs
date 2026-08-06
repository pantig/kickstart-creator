namespace KickstartCreator.Web.Models;

/// <summary>
/// How the target installation disk is determined. <see cref="Interactive"/> is
/// the default: nobody can reliably predict /dev/sda vs /dev/sdb vs a given
/// LUN/RAID member across heterogeneous hardware, so by default the generated
/// kickstart shows the operator the available disks at install time (%pre) and
/// lets them pick. <see cref="Manual"/> opts into baking a known `by-id` value
/// in ahead of time for fully unattended (e.g. PXE fleet) installs, at the
/// cost of requiring the operator to already know the correct identifier.
/// </summary>
public enum DiskSelectionMode
{
    Interactive,
    Manual,
}
