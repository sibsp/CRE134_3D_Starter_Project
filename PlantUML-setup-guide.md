# Class diagrams for your scripts

You do not need to install anything. You do not need Java, and you do not need an extension.

## Make the picture

1. Copy these two files into your Unity project folder. That is the folder that **contains** `Assets`. Do not put them inside `Assets`.
   - `Make-Class-Diagram.bat`
   - `Make-Class-Diagram.ps1`
2. Double-click **Make-Class-Diagram.bat**.
3. A picture opens. The same picture is saved next to those files as **ClassDiagram.svg**.

Put `ClassDiagram.svg` in your report. In Word: **Insert → Pictures → This device**, then choose that file.

Run the bat again any time you change a script. It overwrites the picture.

## What the picture means

- Each box is one of your classes.
- A hollow arrow means “inherits from”. Most of your scripts point at `MonoBehaviour`.
- A plain arrow means one class has a field of that other class.
- `+` means public.
- `{static}` means it belongs to the class, not to one object.

Only your own scripts are included. TextMesh Pro, Starter Assets, and the other imported packs are left out.

## If it does not work

A black window will stay open and tell you which of these it is.

- **The window says to put the file next to Assets.** The bat is in the wrong folder. Move both files up until they sit beside the `Assets` folder, then double-click the bat again.
- **The window says the picture could not be downloaded.** The PC is not on the internet, or the lab network is blocking `www.plantuml.com`. Connect, or try a phone hotspot, then double-click the bat again.
- **Double-click opened the file in Notepad.** You opened `Make-Class-Diagram.ps1`. Close Notepad and double-click `Make-Class-Diagram.bat` instead.
- **Windows shows a blue warning.** Click **More info**, then **Run anyway**.

You need both files. Copying only the bat will not work.
