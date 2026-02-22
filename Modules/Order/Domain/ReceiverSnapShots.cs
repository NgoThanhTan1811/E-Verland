public sealed record ReceiverSnapshot(string Name, string Phone, string Address)
{
    public static ReceiverSnapshot Create(string name, string phone, string address)
    {
        name = name?.Trim() ?? "";
        phone = phone?.Trim() ?? "";
        address = address?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Receiver name is required");
        if (string.IsNullOrWhiteSpace(phone)) throw new ArgumentException("Receiver phone is required");
        if (string.IsNullOrWhiteSpace(address)) throw new ArgumentException("Receiver address is required");

        return new ReceiverSnapshot(name, phone, address);
    }
}