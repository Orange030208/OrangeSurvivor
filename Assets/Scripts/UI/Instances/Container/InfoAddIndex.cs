public readonly struct InfoAddIndex<T>
{
    public InfoAddIndex(T info, int index)
    {
        this.info = info;
        this.index = index;
    }

    public readonly T info;
    public readonly int index;
}
