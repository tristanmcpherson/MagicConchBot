ssh -t bot './kill.sh'
rsync -avz -e "ssh bot" "/cygdrive/C/Users/shred/OneDrive/Documents/Visual Studio 2017/Projects/MagicConchBot/MagicConchBot/bin/Release/netcoreapp1.1/publish/" :/home/bot/conch
ssh -t bot './run.sh'