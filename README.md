# Outer Wilds Hug Mod
![HugMod Thumbnail](https://user-images.githubusercontent.com/127029039/224515568-b92f0b51-b962-4cd8-b1dd-c279bc8f4a78.png)

 <b>Adds a hug mechanic to the game and unlocks a new DLC-exclusive option in the settings menu.</b>  
 DLC not required, but there are spoilers in the source code.

 Huge thanks to [JohnCorby](https://github.com/JohnCorby), [Vesper](https://github.com/Vesper-Works), and the [Outer Wilds modding community](https://discord.gg/9vE5aHxcF9) for their coding help and support, and [Tephirax](https://github.com/Tephirax) for the screenshot used in the thumbnail!

### How to use the API
 This mod comes with an API, allowing other mods to use its mechanics to make their custom content huggable!  
 - To introduce hug functionality into your mod, add [this .cs file](https://github.com/VioVayo/OWHugMod/blob/main/HugMod/IHugModApi.cs) to your project.  
 - Access the API by calling something like `var hugApi = ModHelper.Interaction.TryGetModApi<IHugModApi>("VioVayo.HugMod");`.  
   You might also have to add `using HugMod;` at the top of your file.
 - Use the methods defined in the interface to add and customise the hug script to fit specific characters or objects.
 - All methods included in the .cs file come with summaries that explain what they do, but if anything's unclear and you have questions, feel free to join the discord linked above and ask.
