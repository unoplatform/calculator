@echo off
REM Builds the native CalcManager engine as a desktop-CRT DLL for the Win32/Skia head.
REM
REM The CalcManager.dll that ships in the repo imports MSVCP140_APP.dll (the UWP CRT) and can
REM therefore never load in a desktop process - this is the replacement.
REM
REM Every upstream engine source compiles unmodified; the only fork-owned C++ is src\CCalcManager.cpp,
REM the C ABI the managed P/Invoke layer binds to. Superseded once uno/native/CMakeLists.txt lands.

setlocal
for %%i in ("%~dp0..\..") do set "ROOT=%%~fi"
set "SRC=%ROOT%\src\CalcManager"
set "SHIM=%ROOT%\uno\native\src"
set "OUT=%ROOT%\uno\native\bin\win32"
set "OBJ=%ROOT%\uno\native\obj\win32"

if not exist "%OBJ%" mkdir "%OBJ%"
if not exist "%OUT%" mkdir "%OUT%"

call "C:\Program Files\Microsoft Visual Studio\18\Enterprise\VC\Auxiliary\Build\vcvars64.bat" >nul
if errorlevel 1 (echo Could not initialise MSVC. & exit /b 1)

pushd "%OBJ%"

echo === compiling ===
REM UnitConverter.cpp and NumberFormattingUtils.cpp are deliberately absent: unit conversion has no
REM native ABI at all - it is a pure-managed port in uno/interop.
cl /nologo /c /EHsc /std:c++17 /O2 /MD /DNDEBUG /D_WINDOWS /D_USRDLL /DCALCMANAGERWIN32_EXPORTS ^
   /FI"%SRC%\pch.h" /I"%SRC%" /I"%SRC%\Header Files" /I"%SRC%\CEngine" /I"%SRC%\Ratpack" /I"%SHIM%" ^
   "%SHIM%\CCalcManager.cpp" ^
   "%SRC%\CalculatorHistory.cpp" "%SRC%\CalculatorManager.cpp" "%SRC%\ExpressionCommand.cpp" ^
   "%SRC%\CEngine\*.cpp" "%SRC%\Ratpack\*.cpp"
if errorlevel 1 (popd & echo Compile failed. & exit /b 1)

echo === linking ===
link /nologo /DLL /OUT:"%OUT%\CalcManager.dll" *.obj
if errorlevel 1 (popd & echo Link failed. & exit /b 1)

popd
echo === %OUT%\CalcManager.dll ===
endlocal
