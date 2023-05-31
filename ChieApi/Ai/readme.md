Characters can be created in this folder by creating subfolders for each Characters

Each subfolder optionally contains a Configuration.json file as well as a Prompt.txt and Start.txt

Any Configuration.json file in the Character folder will merge with the one in this directory, allowing for overrides. 

Any Prompt.txt and Start.txt in this directory will be used as the default, unless otherwise specified within the character directory

Bender/Configuration.json
Bender/Prompt.txt
Bender/Start.txt
Configuration.json
Prompt.txt
Start.txt

Configuration.json contains (some) llama.cpp arguments to use when initializing the client
Prompt.txt populates the Llama.cpp -p tag
Start.txt is streamed into Main.exe as the first input, allowing for what functions as part of the initial prompt, that isn't saved when the context rolls forward. Good for text to start a conversation that doesn't need to be remembered later.