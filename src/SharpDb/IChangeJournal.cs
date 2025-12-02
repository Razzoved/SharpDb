namespace SharpDb;

public interface IChangeJournal
{
    void Start();
    void Stop();
    void Restore();
}
