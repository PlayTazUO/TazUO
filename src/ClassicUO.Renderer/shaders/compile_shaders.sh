#!/bin/bash
#winetricks dxsdk_jun2010
wine fxc.exe /T fx_2_0 /O3 IsometricWorld.fx
wine fxc.exe /T fx_2_0 /O3 /Fo IsometricWorld.fxc IsometricWorld.fx
wine fxc.exe /T fx_2_0 /O3 /Fo xBR.fxc xBR.fx
wine fxc.exe /T fx_2_0 /O3 /Fo FSR.fxc FSR.fx
