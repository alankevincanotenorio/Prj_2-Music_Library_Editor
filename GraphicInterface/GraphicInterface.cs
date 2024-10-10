﻿using Gtk;
using ControllerApp;
using System.Threading.Tasks;

class GraphicInterface : Window
{
    public Controller app = new Controller();  
    private TextView rolasList;
    private Label currentPathLabel;
    private CssProvider cssProvider = new CssProvider();
    private TextView errorLogView;
    private Button changeDirButton;
    private Button miningButton;
    private Button editRolaButton;
    private Button editAlbumButton;
    private Button definePerformerButton;
    private Button addPersonButton;
    private Button searchButton;
    private Button helpButton;

    private ProgressBar progressBar;
    private int totalFiles = 0;


    public GraphicInterface() : base("Music Library Editor")
    {
        SetDefaultSize(800, 600);
        SetPosition(WindowPosition.Center);
        BorderWidth = 10;
        Resizable = true;

        //to change background color
        cssProvider.LoadFromData(@"
            textview {
                background-color: #a9a9a9;
                color: #000000;
            }
            text {
                background-color: #a9a9a9;
                color: #282828;
            }
            window {
                background-color: #999999;
            }
            entry {
                background-color: #ffffff;
                color: #000000;
            }
            frame {
                background-color: #ffffff;
                color: #000000;
            }
        ");
        StyleContext.AddProviderForScreen(Gdk.Screen.Default, cssProvider, 800);

        //close app
        DeleteEvent += (sender, args) => Application.Quit();

        Box mainBox = new Box(Orientation.Vertical, 10);

        Grid grid = new Grid();
        grid.RowSpacing = 10;
        grid.ColumnSpacing = 10;
        grid.Margin = 10;

        // show current path
        currentPathLabel = new Label($"Current Path: {app.GetCurrentPath()}");
        Frame currentPathFrame = new Frame();
        currentPathFrame.Add(currentPathLabel);
        currentPathFrame.ShadowType = ShadowType.EtchedIn;
        currentPathFrame.StyleContext.AddProvider(cssProvider, uint.MaxValue);
        Box pathBox = new Box(Orientation.Horizontal, 10);
        pathBox.PackStart(currentPathFrame, true, true, 5);

        changeDirButton = new Button("Change Path");
        changeDirButton.SetSizeRequest(100, 40);
        changeDirButton.Clicked += OnChangeDirClick!;
        pathBox.PackStart(changeDirButton, false, false, 5);
        grid.Attach(pathBox, 0, 0, 4, 1);

        // TextView to show mined rolas inside a ScrolledWindow
        ScrolledWindow scrolledWindow = new ScrolledWindow();
        scrolledWindow.Vexpand = true;
        scrolledWindow.Hexpand = true;

        rolasList = new TextView();
        rolasList.StyleContext.AddProvider(cssProvider, uint.MaxValue);
        rolasList.Buffer.Text = "Rolas will be displayed here...";
        rolasList.WrapMode = WrapMode.Word;
        rolasList.Editable = false;

        scrolledWindow.Add(rolasList);
        grid.Attach(scrolledWindow, 0, 1, 3, 4);

        Box buttonBox = new Box(Orientation.Vertical, 10);

        // "Start mining"
        progressBar = new ProgressBar();
        miningButton = new Button("Start Mining");
        miningButton.SetSizeRequest(100, 40);
        miningButton.Clicked += OnStartMiningClick!;
        buttonBox.PackStart(miningButton, false, false, 0);
        buttonBox.PackStart(progressBar, false, false, 0);

        // "Edit Rola"
        editRolaButton = new Button("Edit rola");
        editRolaButton.SetSizeRequest(100, 40);
        editRolaButton.Clicked += OnEditRolaClick!;
        buttonBox.PackStart(editRolaButton, false, false, 0);

        // "Edit album"
        editAlbumButton = new Button("Edit album");
        editAlbumButton.SetSizeRequest(100, 40);
        editAlbumButton.Clicked += OnEditAlbumButton!;
        buttonBox.PackStart(editAlbumButton, false, false, 0);

        // "Define performer"
        definePerformerButton = new Button("Define Performer");
        definePerformerButton.SetSizeRequest(100, 40);
        definePerformerButton.Clicked += OnDefinePerformerButton!;
        buttonBox.PackStart(definePerformerButton, false, false, 0);

        // "Add person to group"
        addPersonButton = new Button("Add person to group");
        addPersonButton.SetSizeRequest(100, 40);
        buttonBox.PackStart(addPersonButton, false, false, 0);

        // "Search"
        searchButton = new Button();
        Image searchIcon = new Image(Stock.Find, IconSize.Button);
        searchButton.Image = searchIcon;
        searchButton.SetSizeRequest(40, 40);
        buttonBox.PackStart(searchButton, false, false, 0);

        // "Help"
        helpButton = new Button("Help");
        helpButton.SetSizeRequest(100, 40);
        buttonBox.PackStart(helpButton, false, false, 0);
    
        // log view
        errorLogView = new TextView();
        errorLogView.Buffer.Text = "Log:\n";
        errorLogView.Editable = false;
        ScrolledWindow errorLogScrolledWindow = new ScrolledWindow();
        errorLogScrolledWindow.Add(errorLogView);
        grid.Attach(errorLogScrolledWindow, 0, 5, 4, 1);

        grid.Attach(buttonBox, 3, 1, 1, 5);

        // add grid
        mainBox.PackStart(grid, true, true, 0);
        Add(mainBox);

        if (app.GetDataBase().IsRolasTableEmpty()) DisableNonMiningActions();
        else 
        {
            List<string> rolas = app.GetRolasInfoInPath();
            rolasList.Buffer.Text = string.Join("\n", rolas);
        }

        ShowAll();
    }

    // manage change directory
    void OnChangeDirClick(object sender, EventArgs args)
    {
        Window changePathWindow = new Window("Change Path");
        changePathWindow.SetDefaultSize(300, 100);
        changePathWindow.SetPosition(WindowPosition.Center);
        changePathWindow.StyleContext.AddProvider(cssProvider, 800);

        Box vbox = new Box(Orientation.Vertical, 10);
        Label instructionLabel = new Label("Insert the new path:");
        vbox.PackStart(instructionLabel, false, false, 5);

        Entry pathEntry = new Entry();
        vbox.PackStart(pathEntry, false, false, 5);
        pathEntry.StyleContext.AddProvider(cssProvider, uint.MaxValue);

        Button confirmButton = new Button("Confirm");
        confirmButton.Clicked += (s, e) => {            
            bool isValidPath = app.SetCurrentPath(pathEntry.Text);
            if(!isValidPath)
            {
                changePathWindow.Hide();
                MessageDialog errorDialog = new MessageDialog(this,DialogFlags.Modal,MessageType.Error,ButtonsType.Ok,"Invalid path. Please enter a valid directory.");
                errorDialog.Run();
                errorDialog.Hide();
                return;
            }
            currentPathLabel.Text = $"Current Path: {pathEntry.Text}";
            changePathWindow.Hide();
        };
        vbox.PackStart(confirmButton, false, false, 5);
        changePathWindow.Add(vbox);
        changePathWindow.ShowAll();
    }


    private async void OnStartMiningClick(object sender, EventArgs e)
    {
        miningButton.Sensitive = false;
        totalFiles = app.GetTotalMp3FilesInPath();
        progressBar.Fraction = 0;
        if(totalFiles == 0)
        {
            progressBar.Fraction = 1;
            progressBar.Text = "100%";   
        }
        await Task.Run(() => 
        {
            app.StartMining((processedFiles) =>
            {
                Application.Invoke(delegate
                {
                    float progress = (float)processedFiles / totalFiles;
                    progressBar.Fraction = progress;
                    progressBar.Text = $"{(int)(progress * 100)}%";
                });
            });
        });
        app.SetProcessedFilesNumber(0);
        List<string> rolas = app.GetRolasInfoInPath();
        rolasList.Buffer.Text = string.Join("\n", rolas);
        if (app.GetLog().Count > 0)
            errorLogView.Buffer.Text = "Log:\n" + string.Join("\n", app.GetMiner().GetLog());
        miningButton.Sensitive = true;
    }

    void OnEditRolaClick(object sender, EventArgs e)
    {
        Window editRola = new Window("Edit Rola");
        editRola.SetDefaultSize(300, 100);
        editRola.SetPosition(WindowPosition.Center);
        editRola.StyleContext.AddProvider(cssProvider, 800);

        Box vbox = new Box(Orientation.Vertical, 10);
        Label instructionLabel = new Label("Enter the rola title:");
        vbox.PackStart(instructionLabel, false, false, 5);

        Entry entry = new Entry();
        vbox.PackStart(entry, false, false, 5);
        entry.StyleContext.AddProvider(cssProvider, uint.MaxValue);

        Button confirm = new Button("Confirm");
        vbox.PackStart(confirm, false, false, 5);

        confirm.Clicked += (s, e) => {
            string rolaTitle = entry.Text;
            List<string> rolasOptions = app.GetRolasOptions(rolaTitle);
            if (rolasOptions.Count == 0)
            {
                MessageDialog errorDialog = new MessageDialog(editRola, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "No rola found with that title. Please enter a valid title.");
                errorDialog.Run();
                errorDialog.Hide();
            }
            else if (rolasOptions.Count == 1)
            {
                editRola.Hide();
                List<string> rolaDetails = app.GetRolaDetails(rolaTitle, rolasOptions.First());
                ShowEditForm(rolaTitle, rolasOptions.First(), rolaDetails);
            }
            else
            {
                editRola.Hide();
                Window selectionWindow = new Window("Select Rola");
                selectionWindow.SetDefaultSize(400, 300);
                selectionWindow.SetPosition(WindowPosition.Center);
                selectionWindow.StyleContext.AddProvider(cssProvider, 800);

                Box selectionVbox = new Box(Orientation.Vertical, 10);
                Label selectLabel = new Label("Select the Rola to edit:");
                selectionVbox.PackStart(selectLabel, false, false, 5);

                foreach (var rolaPath in rolasOptions)
                {
                    List<string> rolaDetails = app.GetRolaDetails(rolaTitle, rolaPath);
                    string rolaInfo = $"Title: {rolaDetails[0]} \nGenre: {rolaDetails[1]} \nTrack: {rolaDetails[2]} \nPerformer: {rolaDetails[3]} \nYear: {rolaDetails[4]} \nAlbum: {rolaDetails[5]} \nPath: {rolaPath}";
                    Button rolaButton = new Button(rolaInfo);
                    selectionVbox.PackStart(rolaButton, false, false, 5);

                    rolaButton.Clicked += (sender, args) => 
                    {
                        selectionWindow.Hide();
                        ShowEditForm(rolaTitle, rolaPath, rolaDetails);
                    };
                }
                selectionWindow.Add(selectionVbox);
                selectionWindow.ShowAll();
            }
        };
        editRola.Add(vbox);
        editRola.ShowAll();
    }

    void ShowEditForm(string rolaTitle, string rolaPath, List<string> rolaDetails)
    {
        Window detailsWindow = new Window("Edit Rola");
        detailsWindow.SetDefaultSize(300, 400);
        detailsWindow.SetPosition(WindowPosition.Center);
        detailsWindow.StyleContext.AddProvider(cssProvider, 800);

        Box detailsBox = new Box(Orientation.Vertical, 10);

        Entry newTitleEntry = new Entry { Text = rolaDetails[0] };
        Entry newGenreEntry = new Entry { Text = rolaDetails[1] };
        Entry newTrackEntry = new Entry { Text = rolaDetails[2] };
        Entry performerEntry = new Entry { Text = rolaDetails[3] };
        Entry newYearEntry = new Entry { Text = rolaDetails[4] };
        Entry newAlbumEntry = new Entry { Text = rolaDetails[5] }; 

        Label pathLabel = new Label($"Path: {rolaPath}");
        detailsBox.PackStart(new Label("Current Path:"), false, false, 5);
        detailsBox.PackStart(pathLabel, false, false, 5);

        detailsBox.PackStart(new Label("New Title:"), false, false, 5);
        detailsBox.PackStart(newTitleEntry, false, false, 5);

        detailsBox.PackStart(new Label("New Genre:"), false, false, 5);
        detailsBox.PackStart(newGenreEntry, false, false, 5);

        detailsBox.PackStart(new Label("New Track Number:"), false, false, 5);
        detailsBox.PackStart(newTrackEntry, false, false, 5);

        detailsBox.PackStart(new Label("New Performer Name:"), false, false, 5);
        detailsBox.PackStart(performerEntry, false, false, 5);

        detailsBox.PackStart(new Label("New Year:"), false, false, 5);
        detailsBox.PackStart(newYearEntry, false, false, 5);

        detailsBox.PackStart(new Label("New Album Name:"), false, false, 5);
        detailsBox.PackStart(newAlbumEntry, false, false, 5);

        Button acceptButton = new Button("Accept");
        detailsBox.PackStart(acceptButton, false, false, 5);

        acceptButton.Clicked += (sender, eventArgs) =>
        {
            MessageDialog errorDialog;
            if (!int.TryParse(newTrackEntry.Text, out int trackNumber))
            {
                errorDialog = new MessageDialog(detailsWindow, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "Track number must be an integer.");
                errorDialog.Run();
                errorDialog.Hide();
                return;
            }

            if (!int.TryParse(newYearEntry.Text, out int year))
            {
                errorDialog = new MessageDialog(detailsWindow, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "Year must be an integer.");
                errorDialog.Run();
                errorDialog.Hide();
                return;
            }

            app.UpdateRolaDetails(
                rolaTitle,
                rolaPath,
                newTitleEntry.Text, 
                newGenreEntry.Text, 
                newTrackEntry.Text, 
                performerEntry.Text, 
                newYearEntry.Text, 
                newAlbumEntry.Text
            );

            MessageDialog successDialog = new MessageDialog(detailsWindow, DialogFlags.Modal, MessageType.Info, ButtonsType.Ok, "Rola updated successfully.");
            successDialog.Run();
            successDialog.Hide();

            detailsWindow.Hide();
            detailsWindow.Dispose(); // Liberar recursos de la ventana

        };
        detailsWindow.Add(detailsBox);
        detailsWindow.ShowAll();
    }


    void OnEditAlbumButton(object sender, EventArgs e)
    {
        Window editAlbum = new Window("Edit Album");
        editAlbum.SetDefaultSize(300, 100);
        editAlbum.SetPosition(WindowPosition.Center);
        editAlbum.StyleContext.AddProvider(cssProvider, 800);

        Box vbox = new Box(Orientation.Vertical, 10);
        Label instructionLabel = new Label("Enter the album name:");
        vbox.PackStart(instructionLabel, false, false, 5);
        Entry entry = new Entry();
        vbox.PackStart(entry, false, false, 5);
        entry.StyleContext.AddProvider(cssProvider, uint.MaxValue);

        Button confirm = new Button("Confirm");
        vbox.PackStart(confirm, false, false, 5);

        confirm.Clicked += (s, e) =>
        {
            string albumName = entry.Text;
            List<string> albumsOptions = app.GetAlbumsOptions(albumName);
            if(albumsOptions.Count == 0)
            {
                MessageDialog errorDialog = new MessageDialog(editAlbum, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "No album found with that name. Please enter a valid name.");
                errorDialog.Run();
                errorDialog.Hide();
            }
            else if (albumsOptions.Count == 1)
            {
                editAlbum.Hide();
                List<string> albumDetails = app.GetAlbumDetails(albumName, albumsOptions.First());
                ShowEditAlbumForm(albumName, albumsOptions.First(), albumDetails);
            }
            else
            {
                editAlbum.Hide();
                Window selectionWindow = new Window("Select Album");
                selectionWindow.SetDefaultSize(400, 300);
                selectionWindow.SetPosition(WindowPosition.Center);
                selectionWindow.StyleContext.AddProvider(cssProvider, 800);

                Box selectionVbox = new Box(Orientation.Vertical, 10);
                Label selectLabel = new Label("Select the Album to edit:");
                selectionVbox.PackStart(selectLabel, false, false, 5);

                foreach (var albumPath in albumsOptions)
                {
                    List<string> albumDetails = app.GetAlbumDetails(albumName, albumsOptions.First());
                    string albumInfo = $"Name: {albumDetails[0]} \nYear: {albumDetails[1]} \nPath: {albumPath}";
                    Button albumButton = new Button(albumInfo);
                    selectionVbox.PackStart(albumButton, false, false, 5);

                    albumButton.Clicked += (sender, args) => 
                    {
                        selectionWindow.Hide();
                        ShowEditAlbumForm(albumName, albumPath, albumDetails);
                    };
                }
                selectionWindow.Add(selectionVbox);
                selectionWindow.ShowAll();
            }
        };
        editAlbum.Add(vbox);
        editAlbum.ShowAll();
    }


    void ShowEditAlbumForm(string albumName, string albumPath, List<string> albumDetails)
    {
        Window detailsWindow = new Window("Edit Album");
        detailsWindow.SetDefaultSize(300, 400);
        detailsWindow.SetPosition(WindowPosition.Center);
        detailsWindow.StyleContext.AddProvider(cssProvider, 800);

        Box detailsBox = new Box(Orientation.Vertical, 10);

        Entry newNameEntry = new Entry { Text = albumDetails[0] };
        Entry newYearEntry = new Entry { Text = albumDetails[1] }; 

        Label pathLabel = new Label($"Path: {albumPath}");
        detailsBox.PackStart(new Label("Current Path:"), false, false, 5);
        detailsBox.PackStart(pathLabel, false, false, 5);

        detailsBox.PackStart(new Label("New Name:"), false, false, 5);
        detailsBox.PackStart(newNameEntry, false, false, 5);

        detailsBox.PackStart(new Label("New Year:"), false, false, 5);
        detailsBox.PackStart(newYearEntry, false, false, 5);

        Button acceptButton = new Button("Accept");
        detailsBox.PackStart(acceptButton, false, false, 5);

        acceptButton.Clicked += (sender, eventArgs) =>
        {
            MessageDialog errorDialog;
            if (!int.TryParse(newYearEntry.Text, out int year))
            {
                errorDialog = new MessageDialog(detailsWindow, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "Year must be an integer.");
                errorDialog.Run();
                errorDialog.Hide();
                return;
            }

            app.UpdateAlbumDetails(
                albumName,
                newNameEntry.Text,
                albumPath,
                newYearEntry.Text
            );

            MessageDialog successDialog = new MessageDialog(detailsWindow, DialogFlags.Modal, MessageType.Info, ButtonsType.Ok, "Rola updated successfully.");
            successDialog.Run();
            successDialog.Hide();

            detailsWindow.Hide();
        };
        detailsWindow.Add(detailsBox);
        detailsWindow.ShowAll();
    }


    void OnDefinePerformerButton(object sender, EventArgs e)
    {
        Window definePerformer = new Window("Define Performer");
        definePerformer.SetDefaultSize(300, 200);
        definePerformer.SetPosition(WindowPosition.Center);
        definePerformer.StyleContext.AddProvider(cssProvider, 800);

        Box vbox = new Box(Orientation.Vertical, 10);

        Label performerLabel = new Label("Enter performer name:");
        Entry performerEntry = new Entry();
        vbox.PackStart(performerLabel, false, false, 5);
        vbox.PackStart(performerEntry, false, false, 5);

        Label instructionLabel = new Label("Define performer as:");
        vbox.PackStart(instructionLabel, false, false, 5);

        Button personButton = new Button("Person");
        vbox.PackStart(personButton, false, false, 5);

        Button groupButton = new Button("Group");
        vbox.PackStart(groupButton, false, false, 5);

        personButton.Clicked += (s, e) =>
        {
            string performerName = performerEntry.Text;
            if (!string.IsNullOrEmpty(performerName) && app.ExistsPerformer(performerName))
            {
                if(app.IsDefined(performerName))
                {
                    if(app.GetTypePerformer(performerName) == 1)
                    {
                        MessageDialog redefine = new MessageDialog(definePerformer, DialogFlags.Modal, MessageType.Question, ButtonsType.YesNo, "This performer is already defined as group");
                        redefine.Run();
                        redefine.Hide();   
                    }
                    else 
                    {
                        MessageDialog redefineDialog = new MessageDialog(definePerformer, DialogFlags.Modal, MessageType.Question, ButtonsType.YesNo, "This performer is already defined. Do you want to redefine it?");
                        ResponseType response = (ResponseType)redefineDialog.Run();
                        redefineDialog.Hide();

                        if(response == ResponseType.Yes)
                        {
                            definePerformer.Hide();
                            DefinePerson(performerName);
                        }
                        else definePerformer.Hide();
                    }
                }
                definePerformer.Hide();
                DefinePerson(performerName);
            }
            else
            {
                MessageDialog errorDialog = new MessageDialog(definePerformer, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "Please enter a valid performer name.");
                errorDialog.Run();
                errorDialog.Hide();
            }
        };

        groupButton.Clicked += (s, e) =>
        {
            string performerName = performerEntry.Text;
            if (!string.IsNullOrEmpty(performerName) && app.ExistsPerformer(performerName))
            {
                if(app.IsDefined(performerName))
                {
                     if(app.GetTypePerformer(performerName) == 0)
                    {
                        MessageDialog redefinen = new MessageDialog(definePerformer, DialogFlags.Modal, MessageType.Question, ButtonsType.YesNo, "This performer is already defined as person");
                        redefinen.Run();
                        redefinen.Hide();
                    }
                    else
                    {
                        MessageDialog redefineDialog = new MessageDialog(definePerformer, DialogFlags.Modal, MessageType.Question, ButtonsType.YesNo, "This performer is already defined. Do you want to redefine it?");
                        ResponseType response = (ResponseType)redefineDialog.Run();
                        redefineDialog.Hide();

                        if(response == ResponseType.Yes)
                        {
                            definePerformer.Hide();
                            DefineGroup(performerName);
                        }
                        else definePerformer.Hide();
                    }
                }
                else
                {
                    definePerformer.Hide();
                    DefineGroup(performerName);
                }
            }
            else
            {
                MessageDialog errorDialog = new MessageDialog(definePerformer, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "Please enter a  valid performer name.");
                errorDialog.Run();
                errorDialog.Hide();
            }
        };

        definePerformer.Add(vbox);
        definePerformer.ShowAll();
    }

    void DefinePerson(string performerName)
    {
        Window personWindow = new Window("Define Person");
        personWindow.SetDefaultSize(300, 400);
        personWindow.SetPosition(WindowPosition.Center);
        personWindow.StyleContext.AddProvider(cssProvider, 800);

        Box detailsBox = new Box(Orientation.Vertical, 10);
        Label stageNameLabel = new Label("Stage Name:");
        Entry stageNameEntry = new Entry
        {
            Text = performerName,
            Sensitive = false
        };
        detailsBox.PackStart(stageNameLabel, false, false, 5);
        detailsBox.PackStart(stageNameEntry, false, false, 5);

        Label realNameLabel = new Label("Real Name:");
        Entry realNameEntry = new Entry();
        detailsBox.PackStart(realNameLabel, false, false, 5);
        detailsBox.PackStart(realNameEntry, false, false, 5);

        Label birthDateLabel = new Label("Birth Date:");
        Entry birthDateEntry = new Entry();
        detailsBox.PackStart(birthDateLabel, false, false, 5);
        detailsBox.PackStart(birthDateEntry, false, false, 5);

        Label deathDateLabel = new Label("Death Date (optional):");
        Entry deathDateEntry = new Entry();
        detailsBox.PackStart(deathDateLabel, false, false, 5);
        detailsBox.PackStart(deathDateEntry, false, false, 5);

        Button confirmButton = new Button("Confirm");
        detailsBox.PackStart(confirmButton, false, false, 5);

        
        confirmButton.Clicked += (s, e) =>
        {
            string stageName = stageNameEntry.Text;
            string realName = realNameEntry.Text;
            string birthDate = birthDateEntry.Text;
            string deathDate = deathDateEntry.Text;

            if (!string.IsNullOrEmpty(stageName) && !string.IsNullOrEmpty(realName) && !string.IsNullOrEmpty(birthDate))
            {
                app.DefinePerformerAsPerson(performerName, stageName, realName, birthDate, deathDate);
                MessageDialog successDialog = new MessageDialog(personWindow, DialogFlags.Modal, MessageType.Info, ButtonsType.Ok, "Performer defined as person.");
                successDialog.Run();
                successDialog.Hide();
                personWindow.Hide();
            }
            else
            {
                MessageDialog errorDialog = new MessageDialog(personWindow, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "Please fill in all required fields.");
                errorDialog.Run();
                errorDialog.Hide();
            }
        };

        personWindow.Add(detailsBox);
        personWindow.ShowAll();
    }

    void DefineGroup(string performerName)
    {
        Window groupWindow = new Window("Define Group");
        groupWindow.SetDefaultSize(300, 400);
        groupWindow.SetPosition(WindowPosition.Center);
        groupWindow.StyleContext.AddProvider(cssProvider, 800);

        Box detailsBox = new Box(Orientation.Vertical, 10);

        Label groupNameLabel = new Label("Group Name:");
        Entry groupNameEntry = new Entry
        {
            Text = performerName,
            Sensitive = false
        };
        detailsBox.PackStart(groupNameLabel, false, false, 5);
        detailsBox.PackStart(groupNameEntry, false, false, 5);

        Label startDateLabel = new Label("Start Date:");
        Entry startDateEntry = new Entry();
        detailsBox.PackStart(startDateLabel, false, false, 5);
        detailsBox.PackStart(startDateEntry, false, false, 5);

        Label endDateLabel = new Label("End Date (optional):");
        Entry endDateEntry = new Entry();
        detailsBox.PackStart(endDateLabel, false, false, 5);
        detailsBox.PackStart(endDateEntry, false, false, 5);

        Button confirmButton = new Button("Confirm");
        detailsBox.PackStart(confirmButton, false, false, 5);

        confirmButton.Clicked += (s, e) =>
        {
            string groupName = groupNameEntry.Text;
            string startDate = startDateEntry.Text;
            string endDate = endDateEntry.Text;

            if (!string.IsNullOrEmpty(groupName) && !string.IsNullOrEmpty(startDate))
            {
                app.DefinePerformerAsGroup(performerName, groupName, startDate, endDate);
                MessageDialog successDialog = new MessageDialog(groupWindow, DialogFlags.Modal, MessageType.Info, ButtonsType.Ok, "Performer defined as group.");
                successDialog.Run();
                successDialog.Hide();
                groupWindow.Hide();
            }
            else
            {
                MessageDialog errorDialog = new MessageDialog(groupWindow, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "Please fill in all required fields.");
                errorDialog.Run();
                errorDialog.Hide();
            }
        };

        groupWindow.Add(detailsBox);
        groupWindow.ShowAll();
    }

    private void DisableNonMiningActions()
    {
        editRolaButton.Sensitive = false;
        searchButton.Sensitive = false;
        helpButton.Sensitive = false;
    }

    private void AbleNonMiningActions()
    {
        editRolaButton.Sensitive = true;
        searchButton.Sensitive = true;
        helpButton.Sensitive = true;
    }

    public static void Main()
    {
        Application.Init();
        new GraphicInterface();
        Application.Run();
    }
}