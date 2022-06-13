#pragma once
#ifndef __ST_UnmanagedDLL_h__
#define __ST_UnmanagedDLL_h__

// The following ifdef block is the standard way of creating macros which make exporting 
// from a DLL simpler. All files within this DLL are compiled with the EXAMPLEUNMANAGEDDLL_EXPORTS
// symbol defined on the command line. this symbol should not be defined on any project
// that uses this DLL. This way any other project whose source files include this file see 
// EXAMPLEUNMANAGEDDLL_API functions as being imported from a DLL, whereas this DLL sees symbols
// defined with this macro as being exported.

#define STUNMANAGEDDLL_API __declspec(dllexport)


class STUNMANAGEDDLL_API StarTrackerClass
{
private: //initialize python tetra3 class object
	PyObject* StarTrackerClass_obj;
public:
	StarTrackerClass();
	virtual ~StarTrackerClass();
	void PassInt(int nValue);
	void PassString(char* pchValue);
	void SolveFromImage(const char* img, double* quaternion);
	void SolveFromArray(byte* img_arr, int len, int w, int h, int num_layers, double* quaternion);	
	void SolveFromImageThread(const char* img, double* quaternion);
	void SolveFromArrayThread(byte* img_arr, int len, int w, int h, int num_layers, double* quaternion);
	char* ReturnString();
};

#endif	// __ExampleUnmanagedDLL_h__
