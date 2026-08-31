@echo off
call "C:\Program Files\Microsoft Visual Studio\18\Enterprise\VC\Auxiliary\Build\vcvars64.bat"

set SRC=D:\Work\calculator\src\CalcManager
set OUT=D:\Throwaway\calcmanager-build
if not exist "%OUT%\obj" mkdir "%OUT%\obj"
cd /d "%OUT%\obj"

echo === compiling ===
cl /nologo /c /EHsc /std:c++17 /O2 /MD /DNDEBUG /D_WINDOWS /D_USRDLL /DCALCMANAGERWIN32_EXPORTS /FI"%SRC%\pch.h" /I"%SRC%" /I"%SRC%\CEngine" /I"%SRC%\Ratpack" "%SRC%\CCalcManager.cpp" "%SRC%\CalculatorHistory.cpp" "%SRC%\CalculatorManager.cpp" "%SRC%\ExpressionCommand.cpp" "%SRC%\CEngine\CalcInput.cpp" "%SRC%\CEngine\CalcUtils.cpp" "%SRC%\CEngine\History.cpp" "%SRC%\CEngine\Number.cpp" "%SRC%\CEngine\Rational.cpp" "%SRC%\CEngine\RationalMath.cpp" "%SRC%\CEngine\calc.cpp" "%SRC%\CEngine\scicomm.cpp" "%SRC%\CEngine\scidisp.cpp" "%SRC%\CEngine\scifunc.cpp" "%SRC%\CEngine\scioper.cpp" "%SRC%\CEngine\sciset.cpp" "%SRC%\Ratpack\basex.cpp" "%SRC%\Ratpack\conv.cpp" "%SRC%\Ratpack\exp.cpp" "%SRC%\Ratpack\fact.cpp" "%SRC%\Ratpack\itrans.cpp" "%SRC%\Ratpack\itransh.cpp" "%SRC%\Ratpack\logic.cpp" "%SRC%\Ratpack\num.cpp" "%SRC%\Ratpack\rat.cpp" "%SRC%\Ratpack\support.cpp" "%SRC%\Ratpack\trans.cpp" "%SRC%\Ratpack\transh.cpp"

echo === linking ===
link /nologo /DLL /OUT:"%OUT%\CalcManager.dll" *.obj

echo === done ===
