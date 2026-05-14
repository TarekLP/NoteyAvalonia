using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace NoteToolAvalonia.ViewModels;

public class CreditItem : ObservableObject
{
    private string _name = string.Empty;
    private string _iconPath = string.Empty;
    private string _url = string.Empty;
    private string _role = string.Empty;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string IconPath
    {
        get => _iconPath;
        set => SetProperty(ref _iconPath, value);
    }

    public string Url
    {
        get => _url;
        set => SetProperty(ref _url, value);
    }

    public string Role
    {
        get => _role;
        set => SetProperty(ref _role, value);
    }

    public CreditItem() { }

    public CreditItem(string name, string role, string iconPath, string url)
    {
        Name = name;
        Role = role;
        IconPath = iconPath;
        Url = url;
    }
}

public partial class CreditsViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<CreditItem> _credits = new();

    public CreditsViewModel()
    {
        LoadCredits();
    }

    private void LoadCredits()
    {
        Credits.Add(new CreditItem(
            "Avalonia UI",
            "UI Framework",
            "https://avatars.githubusercontent.com/u/14075148?s=200&v=4",
            "https://avaloniaui.net"
        ));

        Credits.Add(new CreditItem(
            "NadzW",
            "Criticser - Motivator",
            "https://static-cdn.jtvnw.net/jtv_user_pictures/f4fccec4-57c2-429e-a273-1785a59577cf-profile_image-70x70.png",
            "https://nadeemwali.com/"
        ));

        Credits.Add(new CreditItem(
            "Prtapay",
            "Tester - Motivator",
            "https://cdn.bsky.app/img/avatar/plain/did:plc:m4fbbqqqhp3onbp46yus47eo/bafkreid6j66splpzkwfad22ehi2gs23fn453zrlksurto6nivnlyb7qe3y",
            "https://bsky.app/profile/prtapay.bsky.social"
        ));
        Credits.Add(new CreditItem(
            "ZeroZM0",
            "Tester - Criticser",
            "https://storage.modworkshop.net/users/images/9z0rIQ7ju939cqk8L7wFmJYvJtUIOLuMcApNXemE.webp",
            "https://zerozm0.carrd.co/"
        ));
    }
}