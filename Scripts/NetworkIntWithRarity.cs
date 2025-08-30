using LethalLevelLoader;
using Unity.Netcode;

namespace NotezyLib;

/// <summary>
/// Temp until LLL adds their own NetworkSerializable version of IntWithRarity
/// </summary>
public struct NetworkIntWithRarity : INetworkSerializable
{
    public int id;
    public int rarity;

    public NetworkIntWithRarity(int id, int rarity)
    {
        this.id = id;
        this.rarity = rarity;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref id);
        serializer.SerializeValue(ref rarity);
    }

    public IntWithRarity ToIntWithRarity()
    {
        IntWithRarity result = new IntWithRarity();
        result.Add(id, rarity);
        return result;
    }

    public static NetworkIntWithRarity FromIntWithRarity(IntWithRarity intWithRarity)
    {
        return new NetworkIntWithRarity(intWithRarity.id, intWithRarity.rarity);
    }
}
