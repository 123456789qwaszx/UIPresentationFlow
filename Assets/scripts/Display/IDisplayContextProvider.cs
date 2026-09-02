// The boundary where Unity's global display state (Screen.*, Application.platform)
// is allowed to be read. Everything downstream receives a DisplayContext value
// instead of reading those APIs itself.
//
// GetCurrent() captures a snapshot; callers should hold one snapshot for the
// duration of a single Resolve so every rule sees the same input.
public interface IDisplayContextProvider
{
    DisplayContext GetCurrent();
}
