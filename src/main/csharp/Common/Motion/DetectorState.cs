namespace SebastianHaeni.ThermoBox.Common.Motion
{
    public enum DetectorState
    {
        [NextStates(Entry, Exit)] Entry,
        [NextStates(Exit, Nothing)] Exit,
        [NextStates(Entry, Nothing)] Nothing
    }
}
