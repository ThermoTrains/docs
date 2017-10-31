namespace SebastianHaeni.ThermoBox.Common.Motion
{
    public enum DetectorState
    {
        [NextStates(Exit)] Entry,
        [NextStates(Entry, Nothing)] Exit,
        [NextStates(Entry, Nothing)] Nothing
    }
}
