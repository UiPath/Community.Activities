#NoEnv  ; Recommended for performance and compatibility with future AutoHotkey releases.
; #Warn  ; Enable warnings to assist with detecting common errors.
SendMode Input  ; Recommended for new scripts due to its superior speed and reliability.
SetWorkingDir %A_ScriptDir%  ; Ensures a consistent starting directory.
^F7::
Run, notepad.exe file.txt
Send, 7 lines{!}{Enter}
SendInput, inside the CTRL{+}F7 hotkey.
Send ^s
Send !F
Send x
return