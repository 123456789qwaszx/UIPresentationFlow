public interface IUiActionBinder
{
    bool TryBind(WidgetHandle widget, UIActionKey key);
}