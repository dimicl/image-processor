using System.Collections.Concurrent;
using System.Drawing;

namespace Server.Services;

public class ImageSessionStore
{
    private readonly ConcurrentDictionary<Guid, byte[]> _sessions = new();

    public Guid Store(byte[] imageBytes)
    {
        var id = Guid.NewGuid();
        _sessions[id] = imageBytes;
        return id;
    }

    public byte[]? Get(Guid id) => _sessions.TryGetValue(id, out var data) ? data : null;

    public void Update(Guid id, byte[] imageBytes) => _sessions[id] = imageBytes;

    public bool Remove(Guid id) => _sessions.TryRemove(id, out _);
}
