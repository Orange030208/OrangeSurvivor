public interface IDropService
{
    DropRollResult RollDropForSource(DropSourceInfo dropSource, Entity source, int waveNumber);
}
