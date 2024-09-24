namespace InstantGrinder.Core
{
    public interface IGrindObjection
    {
    }

    public record GrindObjectionOverflowItems(int itemCount) : IGrindObjection;

    public record GrindObjectionMultipleGrinds(string[] gridNames) : IGrindObjection;

    public record GrindObjectionUnrecoverable(string[] gridNames) : IGrindObjection;
}