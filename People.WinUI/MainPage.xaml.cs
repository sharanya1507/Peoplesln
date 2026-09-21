using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using People.DataLayer.Interface;
using People.SharedLayer;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace People_WinUI;

/// <summary>
/// The main content page displayed inside the application window.
/// Add your UI logic, event handlers, and data binding here.
/// </summary>
public sealed partial class MainPage : Page
{
    private IPeopleRepository _repository = null!;
    private readonly ObservableCollection<PersonRow> _people = new();
    private readonly ObservableCollection<PersonRow> _deletedPeople = new();
    private int? _selectedPNo;
    private PersonRow? _selectedRow;

    public MainPage()
    {
        InitializeComponent();
        PeopleListView.ItemsSource = _people;
        DeletedPeopleListView.ItemsSource = _deletedPeople;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is IPeopleRepository repository)
        {
            _repository = repository;
            _ = LoadPeopleAsync();
            _ = DemoJoinFunctionsAsync();
        }
    }

    private async Task LoadPeopleAsync()
    {
        try
        {
            Console.WriteLine("WinUI -> Requesting people list (Get)");

            var people = await _repository.GetAllPeopleAsync();

            Console.WriteLine($"WinUI -> Received {people.Count} PersonResponseDto object(s) (Get)");

            _people.Clear();
            foreach (var person in people)
            {
                _people.Add(new PersonRow(person));
            }
        }
        catch (Exception ex)
        {
            ShowValidationMessage($"Could not load people: {ex.Message}");
        }
    }

    // Demonstrates the two learning join functions (F1 and F2) by calling them
    // once at startup and logging what comes back. Not wired to any button.
    private async Task DemoJoinFunctionsAsync()
    {
        Console.WriteLine("WinUI -> Calling GetPeopleUsingFirstJoinAsync (F1)");
        var f1 = await _repository.GetPeopleUsingFirstJoinAsync();
        Console.WriteLine($"WinUI -> F1 returned {f1.Count} PersonResponseDto object(s)");

        Console.WriteLine("WinUI -> Calling GetPeopleUsingSecondJoinAsync (F2)");
        var f2 = await _repository.GetPeopleUsingSecondJoinAsync();
        Console.WriteLine($"WinUI -> F2 returned {f2.Count} PersonResponseDto object(s)");
    }

    private async void ActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string action)
        {
            return;
        }

        switch (action)
        {
            case "Add":
                await AddPersonAsync();
                break;

            case "Update":
                await UpdatePersonAsync();
                break;

            case "Delete":
                await DeletePersonAsync();
                break;

            case "Clear":
                ClearForm();
                break;
        }
    }

    private async Task AddPersonAsync()
    {
        if (!TryBuildRequest(out var request, out var error))
        {
            ShowValidationMessage(error);
            return;
        }

        var confirmed = await ShowConfirmDialogAsync(
            "Confirm Add",
            BuildRequestDetailsText(request),
            "Yes, Add");
        if (!confirmed)
        {
            return;
        }

        try
        {
            Console.WriteLine("WinUI -> Creating PersonRequestDto (Add)");
            Console.WriteLine($"WinUI -> Name: {request.PFName} {request.PSName}, Age: {request.PAge}");

            var response = await _repository.AddPersonAsync(request);

            Console.WriteLine("WinUI -> Received PersonResponseDto (Add)");
            Console.WriteLine($"WinUI -> PNo: {response.PNo}, TotalMarks: {response.TotalMarks}");

            await LoadPeopleAsync();
            ClearForm();
        }
        catch (Exception ex)
        {
            ShowValidationMessage($"Could not add person: {ex.Message}");
        }
    }

    private async Task UpdatePersonAsync()
    {
        if (_selectedPNo is null)
        {
            ShowValidationMessage("Select a person to update first.");
            return;
        }

        if (!TryBuildRequest(out var request, out var error))
        {
            ShowValidationMessage(error);
            return;
        }

        request.PNo = _selectedPNo;

        var confirmed = await ShowConfirmDialogAsync(
            "Confirm Update",
            $"Person No: {_selectedPNo}\n" + BuildRequestDetailsText(request),
            "Yes, Update");
        if (!confirmed)
        {
            return;
        }

        try
        {
            Console.WriteLine("WinUI -> Creating PersonRequestDto (Update)");
            Console.WriteLine($"WinUI -> PNo: {request.PNo}, Name: {request.PFName} {request.PSName}");

            var response = await _repository.UpdatePersonAsync(request);

            Console.WriteLine("WinUI -> Received PersonResponseDto (Update)");
            Console.WriteLine($"WinUI -> PNo: {response.PNo}, TotalMarks: {response.TotalMarks}");

            await LoadPeopleAsync();
            ClearForm();
        }
        catch (Exception ex)
        {
            ShowValidationMessage($"Could not update person: {ex.Message}");
        }
    }

    private async Task DeletePersonAsync()
    {
        if (_selectedPNo is null || _selectedRow is null)
        {
            ShowValidationMessage("Select a person to delete first.");
            return;
        }

        var confirmed = await ShowConfirmDialogAsync(
            "Confirm Delete",
            BuildPersonDetailsText(_selectedRow.Dto),
            "Yes, Delete");
        if (!confirmed)
        {
            return;
        }

        var request = new PersonRequestDto { PNo = _selectedPNo };

        try
        {
            Console.WriteLine("WinUI -> Creating PersonRequestDto (Delete)");
            Console.WriteLine($"WinUI -> PNo: {request.PNo}");

            var response = await _repository.DeletePersonAsync(request);

            Console.WriteLine("WinUI -> Received PersonResponseDto (Delete)");
            Console.WriteLine($"WinUI -> PNo: {response.PNo}");

            // The DataLayer hands back the record it just deleted, so the
            // deleted-people grid is built from that returned object.
            _deletedPeople.Insert(0, new PersonRow(response));

            await LoadPeopleAsync();
            ClearForm();
        }
        catch (Exception ex)
        {
            ShowValidationMessage($"Could not delete person: {ex.Message}");
        }
    }

    private async Task<bool> ShowConfirmDialogAsync(string title, string message, string primaryButtonText)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = primaryButtonText,
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    private static string BuildRequestDetailsText(PersonRequestDto request)
    {
        return $"First Name: {request.PFName}\n" +
               $"Last Name: {request.PSName}\n" +
               $"Age: {request.PAge}\n" +
               $"SSLC Marks: {request.SslcMarks}\n" +
               $"PUC Marks: {request.PucMarks}\n" +
               $"BE Marks: {request.BeMarks?.ToString() ?? "-"}\n" +
               $"ME Marks: {request.MeMarks?.ToString() ?? "-"}\n" +
               $"Hobby 1: {request.Hobby1 ?? "-"}\n" +
               $"Hobby 2: {request.Hobby2 ?? "-"}\n" +
               $"Hobby 3: {request.Hobby3 ?? "-"}";
    }

    private static string BuildPersonDetailsText(PersonResponseDto person)
    {
        return $"Person No: {person.PNo}\n" +
               $"First Name: {person.PFName}\n" +
               $"Last Name: {person.PSName}\n" +
               $"Age: {person.PAge}\n" +
               $"SSLC Marks: {person.SslcMarks}\n" +
               $"PUC Marks: {person.PucMarks}\n" +
               $"BE Marks: {person.BeMarks?.ToString() ?? "-"}\n" +
               $"ME Marks: {person.MeMarks?.ToString() ?? "-"}\n" +
               $"Hobby 1: {person.Hobby1 ?? "-"}\n" +
               $"Hobby 2: {person.Hobby2 ?? "-"}\n" +
               $"Hobby 3: {person.Hobby3 ?? "-"}\n" +
               $"Total Marks: {person.TotalMarks}";
    }

    private void PeopleListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PeopleListView.SelectedItem is not PersonRow row)
        {
            return;
        }

        _selectedPNo = row.PNo;
        _selectedRow = row;

        PNoBox.Text = row.PNo.ToString();
        FirstNameBox.Text = row.PFName;
        LastNameBox.Text = row.PSName;
        AgeBox.Text = row.PAge.ToString();
        SslcMarksBox.Text = row.SslcMarks.ToString();
        PucMarksBox.Text = row.PucMarks.ToString();
        BeMarksBox.Text = row.BeMarksText;
        MeMarksBox.Text = row.MeMarksText;
        Hobby1Box.Text = row.Hobby1;
        Hobby2Box.Text = row.Hobby2;
        Hobby3Box.Text = row.Hobby3;
        TotalMarksBox.Text = row.TotalMarks.ToString();

        AddButton.IsEnabled = false;
        UpdateButton.IsEnabled = true;
        DeleteButton.IsEnabled = true;

        HideValidationMessage();
    }

    private bool TryBuildRequest(out PersonRequestDto request, out string error)
    {
        request = new PersonRequestDto();
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(FirstNameBox.Text) ||
            string.IsNullOrWhiteSpace(LastNameBox.Text) ||
            string.IsNullOrWhiteSpace(AgeBox.Text) ||
            string.IsNullOrWhiteSpace(SslcMarksBox.Text) ||
            string.IsNullOrWhiteSpace(PucMarksBox.Text))
        {
            error = "First Name, Last Name, Age, SSLC Marks and PUC Marks are required.";
            return false;
        }

        if (!int.TryParse(AgeBox.Text, out var age))
        {
            error = "Age must be a whole number.";
            return false;
        }

        if (!int.TryParse(SslcMarksBox.Text, out var sslcMarks))
        {
            error = "SSLC Marks must be a whole number.";
            return false;
        }

        if (!int.TryParse(PucMarksBox.Text, out var pucMarks))
        {
            error = "PUC Marks must be a whole number.";
            return false;
        }

        int? beMarks = null;
        if (!string.IsNullOrWhiteSpace(BeMarksBox.Text))
        {
            if (!int.TryParse(BeMarksBox.Text, out var be))
            {
                error = "BE Marks must be a whole number.";
                return false;
            }
            beMarks = be;
        }

        int? meMarks = null;
        if (!string.IsNullOrWhiteSpace(MeMarksBox.Text))
        {
            if (!int.TryParse(MeMarksBox.Text, out var me))
            {
                error = "ME Marks must be a whole number.";
                return false;
            }
            meMarks = me;
        }

        request.PFName = FirstNameBox.Text.Trim();
        request.PSName = LastNameBox.Text.Trim();
        request.PAge = age;
        request.SslcMarks = sslcMarks;
        request.PucMarks = pucMarks;
        request.BeMarks = beMarks;
        request.MeMarks = meMarks;
        request.Hobby1 = string.IsNullOrWhiteSpace(Hobby1Box.Text) ? null : Hobby1Box.Text.Trim();
        request.Hobby2 = string.IsNullOrWhiteSpace(Hobby2Box.Text) ? null : Hobby2Box.Text.Trim();
        request.Hobby3 = string.IsNullOrWhiteSpace(Hobby3Box.Text) ? null : Hobby3Box.Text.Trim();

        return true;
    }

    private void ClearForm()
    {
        _selectedPNo = null;
        _selectedRow = null;

        PeopleListView.SelectedItem = null;

        PNoBox.Text = string.Empty;
        FirstNameBox.Text = string.Empty;
        LastNameBox.Text = string.Empty;
        AgeBox.Text = string.Empty;
        SslcMarksBox.Text = string.Empty;
        PucMarksBox.Text = string.Empty;
        BeMarksBox.Text = string.Empty;
        MeMarksBox.Text = string.Empty;
        Hobby1Box.Text = string.Empty;
        Hobby2Box.Text = string.Empty;
        Hobby3Box.Text = string.Empty;
        TotalMarksBox.Text = string.Empty;

        AddButton.IsEnabled = true;
        UpdateButton.IsEnabled = false;
        DeleteButton.IsEnabled = false;

        HideValidationMessage();
    }

    private void ShowValidationMessage(string message)
    {
        ValidationMessage.Text = message;
        ValidationMessage.Visibility = Visibility.Visible;
    }

    private void HideValidationMessage()
    {
        ValidationMessage.Text = string.Empty;
        ValidationMessage.Visibility = Visibility.Collapsed;
    }
}
