namespace DBTools.Abstractions
{
    public interface ISqlValidator
    {
        bool IsValidIdentifier(string identifier);
    }
}
