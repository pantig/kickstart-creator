namespace KickstartCreator.Web.Services;

public sealed class GrubPbkdf2Options
{
    public int Iterations { get; set; } = 10000;
    public int SaltLengthBytes { get; set; } = 64;
    public int DerivedKeyLengthBytes { get; set; } = 64;
}
