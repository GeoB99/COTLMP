# Tools needed

The tools needed to contribute to the mod project are the following:

1. Visual Studio 2022 (or later)
2. Unity Editor 2022.3.62f2

Whilst any text editor (Notepad++, Sublime Text, Visual Studio Code or anything of your choice) can be used, it's generally recommended to use **Visual Studio** due to ease of use when configuring the .NET environment in order to compile .NET C# projects.
Any other text editor requires manual configuration of the .NET environment (compiler, packages, C# stuff et al) which is tedious and a waste of time.
The Unity Editor is only needed if you plan to modify or create new UI scenes for the project.

# Mod directory structure

Before contributing to the mod project you must first familiarize yourself with the mod directory structure. The graph below shows a detailed view of how the directory tree looks like.
**NOTE** that the mod is always in development so the directory tree might change in the future!
```
COTLMP
├───.github -- GitHub specific files
├───Assets -- Unity assets of the mod
│   └───UI -- UI/UX specific visual assets of the mod
├───COTLMP -- Core modules of the mod
│   ├───Api -- COTLMP core API methods and routines
│   ├───Data -- Global directory containing COTLMP data headers and fields
│   ├───Debug -- Debug related management code of the mod
│   ├───Game -- Patches, methods and code that modify the gameplay
│   ├───Language -- Directory containing translation files
│   ├───Network -- Client/Server network management of the mod
│   ├───Skins -- Directory containing player skins
│   └───Ui -- UI/UX module of the mod
├───COTLMPServer -- Core dedicated server module of the mod
├───Docs -- Documentation
└───Media -- Media related stuff (images, banners, etc.)
```

# First time contribution

### Forking & cloning the repository
You must fork the repository in order to contribute to the project with your patches. [Follow the steps](https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/working-with-forks/fork-a-repo) on how to fork a repository if you haven't done it so!
Once you forked the repository, clone the forked repository in your PC with the following command:
```
git clone https://github.com/<YOUR_NAME>/COTLMP
```
Where `<YOUR_NAME>` represents your GitHub username of the forked respoistory.

### Compiling the project for the first timep
Compiling the mod with Visual Studio should be straightforward as long as you have installed the **".NET desktop environment"** during installation of the VS IDE. If you haven't done it so you can always install the .NET environment workload at anytime through **Visual Studio Installer**.
When everything is set up, import the `COTLMP.sln` file within working directory which is the core VSSolution file of the project. Visual Studio will import the project and setups the dependencies and everything needed in order to compile the mod.

To compile the mod, click `Build > Build Solution`. This will generate `COTLMP.dll` and `COTLMPServer.dll` on their rrespective `bin` directories.

### Writing your first patch
Once you've familiarized with the directory tree, you can edit or add new files however you like depending on what you want to do. Refer to the [coding style guidelines](CODING_STYLE.md) for more information about the coding style rules.
To commit your changes in a patch you do:
```
git commit -S
```
`-S` flag means that Git will sign your patch with your GPG key on commiting. This is not mandatory when contributing but it's best practice to sign your commits which proves your identity of your work.
If you don't want to sign your commit, simply ommit the `-S` flag. Git will open the default text editor that was chosen when you intalled Git.
An example of a good commit description would be like:
```
[COTLMP:UI] Refactor the playerboard UI

* Resize the playerboard UI based on screen resolution
* Display the Steam profile avatar of each player
* Stylistic changes in code and further documentation
```
`[COTLMP:UI]` is the label of the commit that describes the module of the mod being touched, denoted by its name after the colon. In this case, the `UI` module was touched.
If a commit or pull request touches many modules, simply use the general `[COTLMP]` label.

# Contribution rules

1. Always submit your patches in your own branch, avoid using the `master` branch as your personal branch! Creating your own branch helps organizing your work and also updating your branch on top of remote repository.
Commiting directly to `master` often leads to your work being invertedly lost if you rebase or accidentaly alter the commit history!
2. Always group your work in separate commits! Do not mix feature changes with coding style changes into one single commit!
3. Always be explicit on what you're doing and ensure your contribution is properly described!
4. **AI-slop code** is **NOT** allowed! While using AI tools to aid you with development is generally allowed, you must write the code yourself!
Using AI tools to help on certain things, figuring out how to fix a certain bug or figuring how to implement a certain idea is not bad per se, using AI to do everything for you is forbidden!
