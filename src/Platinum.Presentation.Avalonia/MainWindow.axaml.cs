using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Platinum.Application.Projects;
using Platinum.Domain.Layout;
using Platinum.Infrastructure.Projects;
using Platinum.Presentation.Avalonia.Catalogue;

namespace Platinum.Presentation.Avalonia;

public partial class MainWindow : Window
{
    private readonly ProjectService _projects=new(new PlatinumPackageStore());
    private TrackLayout _layout=CreateNewLayout();
    private string? _currentPath;
    private List<CatalogueItem> _catalogueItems=[];

    public MainWindow()
    {
        InitializeComponent();
        LevelSelector.ItemsSource=_layout.Levels;
        LevelSelector.SelectedIndex=0;
        TrackPlan.SetLayout(_layout,ActiveLevelId);
        LoadCatalogue();
        ProjectStatus.Text="PROJECTE NO DESAT";
    }
    private Guid ActiveLevelId=>LevelSelector.SelectedItem is TrackLevel level?level.Id:_layout.Levels[0].Id;
    private static TrackLayout CreateNewLayout()=>new(){Name="Nova maqueta",Levels=Enumerable.Range(0,5).Select(i=>new TrackLevel(Guid.NewGuid(),"Nivell "+(i+1),i*110m)).ToList()};

    private void NewProject_Click(object? s,Avalonia.Interactivity.RoutedEventArgs e)
    {
        _layout=CreateNewLayout();_currentPath=null;LevelSelector.ItemsSource=_layout.Levels;LevelSelector.SelectedIndex=0;
        TrackPlan.SetLayout(_layout,ActiveLevelId);ProjectStatus.Text="PROJECTE NO DESAT";StatusText.Text="Projecte nou creat.";
    }
    private async void OpenProject_Click(object? s,Avalonia.Interactivity.RoutedEventArgs e)
    {
        var files=await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions{Title="Obrir projecte Platinum",AllowMultiple=false,FileTypeFilter=[new FilePickerFileType("Projecte Quiral Platinum"){Patterns=["*.platinum"]}]});
        var path=files.FirstOrDefault()?.Path.LocalPath;if(string.IsNullOrWhiteSpace(path))return;
        try{_layout=await _projects.LoadAsync(path);if(_layout.Levels.Count==0)_layout.Levels.Add(new TrackLevel(Guid.NewGuid(),"Nivell principal",0m));
            _currentPath=path;LevelSelector.ItemsSource=_layout.Levels;LevelSelector.SelectedIndex=0;TrackPlan.SetLayout(_layout,ActiveLevelId);
            ProjectStatus.Text=Path.GetFileName(path);StatusText.Text="Projecte carregat i validat.";}
        catch(Exception ex){StatusText.Text="Error en obrir el projecte: "+ex.Message;}
    }
    private async void SaveProject_Click(object? s,Avalonia.Interactivity.RoutedEventArgs e)
    {
        var path=_currentPath;
        if(string.IsNullOrWhiteSpace(path))
        {
            var file=await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions{Title="Desar projecte Platinum",SuggestedFileName=_layout.Name,DefaultExtension="platinum",FileTypeChoices=[new FilePickerFileType("Projecte Quiral Platinum"){Patterns=["*.platinum"]}]});
            path=file?.Path.LocalPath;
        }
        if(string.IsNullOrWhiteSpace(path))return;
        try{await _projects.SaveAsync(_layout,path);_currentPath=path;ProjectStatus.Text=Path.GetFileName(path);StatusText.Text="Projecte desat correctament.";}
        catch(Exception ex){StatusText.Text="Error en desar el projecte: "+ex.Message;}
    }
    private void LevelSelector_SelectionChanged(object? s,SelectionChangedEventArgs e){if(TrackPlan is not null&&_layout.Levels.Count>0)TrackPlan.SetActiveLevel(ActiveLevelId);}
    private void SelectTool_Click(object? s,Avalonia.Interactivity.RoutedEventArgs e)=>TrackPlan.SetTool("Seleccionar");
    private void StraightTool_Click(object? s,Avalonia.Interactivity.RoutedEventArgs e)=>TrackPlan.SetTool("Via recta");
    private void CurveTool_Click(object? s,Avalonia.Interactivity.RoutedEventArgs e)=>TrackPlan.SetTool("Corba");
    private void TurnoutTool_Click(object? s,Avalonia.Interactivity.RoutedEventArgs e)=>TrackPlan.SetTool("Desviament");

    private void LoadCatalogue()
    {
        try{var path=Path.Combine(AppContext.BaseDirectory,"Assets","catalogue-marklin-c.json");var doc=MarklinCCatalogue.Load(path);
            _catalogueItems=doc.Items;CatalogueList.ItemsSource=_catalogueItems;TrackPlan.SetCatalogue(_catalogueItems);
            CatalogueCount.Text=_catalogueItems.Count+" referències · "+doc.Brand+" "+doc.System+" · "+doc.Scale;}
        catch(Exception ex){CatalogueCount.Text="No s'ha pogut carregar el catàleg.";CatalogueDetails.Text=ex.Message;}
    }
    private void CatalogueSearch_TextChanged(object? s,TextChangedEventArgs e)
    {
        if(CatalogueList is null)return;var q=CatalogueSearch.Text?.Trim()??"";
        CatalogueList.ItemsSource=string.IsNullOrWhiteSpace(q)?_catalogueItems:_catalogueItems.Where(x=>x.Ref.Contains(q,StringComparison.OrdinalIgnoreCase)||x.Name.Contains(q,StringComparison.OrdinalIgnoreCase)||x.Category.Contains(q,StringComparison.OrdinalIgnoreCase)).ToList();
    }
    private void CatalogueList_SelectionChanged(object? s,SelectionChangedEventArgs e)
    {
        if(CatalogueList.SelectedItem is not CatalogueItem item){PlaceCatalogueButton.IsEnabled=false;return;}
        PlaceCatalogueButton.IsEnabled=true;CatalogueDetails.Text=item.DisplayName+"\nCategoria: "+item.Category+"\nTipus: "+item.Type+"\nGeometria: "+item.GeometrySummary+"\nEstat: "+item.GeometryStatus+"\n"+item.Notes;
    }
    private void PlaceCatalogueButton_Click(object? s,Avalonia.Interactivity.RoutedEventArgs e)
    {
        if(CatalogueList.SelectedItem is not CatalogueItem item)return;
        if(item.GeometryStatus.Equals("unverified",StringComparison.OrdinalIgnoreCase))StatusText.Text="Avís: geometria no verificada; marcador esquemàtic. ";
        TrackPlan.BeginCataloguePlacement(item);StatusText.Text+="Fes clic al plànol per col·locar "+item.Ref+".";
    }
}