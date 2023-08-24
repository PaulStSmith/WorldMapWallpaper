# DesktopImageChanger
Updates the desktop image with a world map depicting the day/night cycle with the time when the program runs. 
Can be automated with a task scheduler.

This is a simple console application that creates and updates the desktop wallpaper with an image similar to this one.
![World map with day/night cycle](https://paulstsmith.github.io/images/worldTimeMap.jpg)

If you are using the task scheduler to run this at specific times, 
you'll need to set the “Run only when user is logged on” option.
Because otherwise, due the way a task runs, the wallpaper will 
not be updated.
