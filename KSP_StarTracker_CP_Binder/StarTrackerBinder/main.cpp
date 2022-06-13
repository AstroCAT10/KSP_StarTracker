
//Ensure that the build is set to "Release x64"!!!

#define PY_SSIZE_T_CLEAN
#include <Windows.h>
#include <stdio.h>
#include <Python.h> //Add "C:\Users\benlu\AppData\Local\Programs\Python\Python37\include" to Project properties --> C/C++ --> General --> Additional Include Directories
#include <iostream>
#include <string>
#include <cinttypes>
#include "main.h"
#include <thread>
using namespace std;

//use DUMPBIN /EXPORTS StarTrackerBinder.dll 

//constructor initializer
StarTrackerClass::StarTrackerClass()
{
    Py_Initialize();

    PyObject* sys = PyImport_ImportModule("sys");
    PyObject* path = PyObject_GetAttrString(sys, "path");
    PyList_Append(path, PyUnicode_FromString("C:\\Users\\benlu\\source\\repos\\KSP_StarTracker_CP_Binder\\python_scripts"));

    PyObject* ST_ModuleString = PyUnicode_FromString((char*)"StarTracker");
    PyObject* ST = PyImport_Import(ST_ModuleString);

    PyObject* dict = PyModule_GetDict(ST);

    PyObject* StarTrackerClass = PyDict_GetItemString(dict, "StarTracker");

    StarTrackerClass_obj = PyObject_CallObject(StarTrackerClass, NULL);
}

StarTrackerClass::~StarTrackerClass()
{
}

void StarTrackerClass::PassInt(int nValue)
{
    std::cout << "PassInt() was called" << std::endl;
    std::cout << "The value is " << nValue << std::endl;
}

void StarTrackerClass::PassString(char* pchValue)
{
    std::cout << "PassString() was called" << std::endl;
    std::cout << "The string is " << pchValue << std::endl;
}

char* StarTrackerClass::ReturnString()
{
    static char s_chString[] = "Hello there";
    std::cout << "ReturnString() was called" << std::endl;

    return s_chString;
}

void StarTrackerClass::SolveFromImage(const char* img, double* quaternion)
{
    std::cout << "SolveFromImage() was called" << std::endl;
    std::cout << "Image file is " << img << std::endl;

    //Py_Initialize();

    PyObject* pyquaternion = PyObject_CallMethod(StarTrackerClass_obj, "SolveFromImage", "(s)", img);

    //int val = PyLong_AsLong(pyval);

    //double quaternion[4];
    int j = 0;
    for (Py_ssize_t i = 0; i < 4; i++) {
        PyObject* next = PyList_GetItem(pyquaternion, i);
        quaternion[j] = PyFloat_AsDouble(next);
        j++;
    }

}

void StarTrackerClass::SolveFromArray(byte* img_arr, int len, int w, int h, int num_layers, double* quaternion)
{
    byte* img = new byte[len];
    memcpy(img, img_arr, len);

    //printf("len = %d\r\n", len);
    //for (int i = 0; i < len; i++) {
    //    printf("%d", img[i]);
    //}
    //printf("\r\n");

    //Py_buffer pybuf;
    //Py_ssize_t len = len;

    //PyBuffer_FillInfo(&pybuf, NULL, img, (Py_ssize_t) len, 1, PyBUF_CONTIG_RO);
    

    PyObject* pyquaternion = PyObject_CallMethod(StarTrackerClass_obj, "SolveFromArray", "(y# i i i i)", img, (Py_ssize_t) len, len, w, h, num_layers);

    //img = (byte*) pybuf.buf;
    //for (int i = 0; i < len; i++) {
    //    printf("%d", img[i]);
    //}
    //printf("\r\n");

    // no other fields are used
    //PyBuffer_Release(&pybuf);

    int j = 0;
    for (Py_ssize_t i = 0; i < 4; i++) {
        PyObject* next = PyList_GetItem(pyquaternion, i);
        quaternion[j] = PyFloat_AsDouble(next);
        j++;
    }

}

void StarTrackerClass::SolveFromImageThread(const char* img, double* quaternion)
{
    std::cout << "SolveFromImageThread() was called" << std::endl;
    std::cout << "Image file is " << img << std::endl;

    //Py_Initialize();

    PyObject* pyquaternion = PyObject_CallMethod(StarTrackerClass_obj, "SolveFromImageThread", "(s)", img);

    //int val = PyLong_AsLong(pyval);

    //double quaternion[4];
    int j = 0;
    for (Py_ssize_t i = 0; i < 4; i++) {
        PyObject* next = PyList_GetItem(pyquaternion, i);
        quaternion[j] = PyFloat_AsDouble(next);
        j++;
    }

}

void StarTrackerClass::SolveFromArrayThread(byte* img_arr, int len, int w, int h, int num_layers, double* quaternion)
{
    byte* img = new byte[len];
    memcpy(img, img_arr, len);

    //printf("len = %d\r\n", len);
    //for (int i = 0; i < len; i++) {
    //    printf("%d", img[i]);
    //}
    //printf("\r\n");

    //Py_buffer pybuf;
    //Py_ssize_t len = len;

    //PyBuffer_FillInfo(&pybuf, NULL, img, (Py_ssize_t)len, 1, PyBUF_CONTIG_RO);


    PyObject* pyquaternion = PyObject_CallMethod(StarTrackerClass_obj, "SolveFromArrayThread", "(y# i i i i)", img, (Py_ssize_t)len, len, w, h, num_layers);

    //img = (byte*) pybuf.buf;
    //for (int i = 0; i < len; i++) {
    //    printf("%d", img[i]);
    //}
    //printf("\r\n");

    // no other fields are used
    //PyBuffer_Release(&pybuf);

    int j = 0;
    for (Py_ssize_t i = 0; i < 4; i++) {
        PyObject* next = PyList_GetItem(pyquaternion, i);
        quaternion[j] = PyFloat_AsDouble(next);
        j++;
    }

}

// export C++ class to C#
extern "C" STUNMANAGEDDLL_API StarTrackerClass* CreateStarTrackerClass()
{
    return new StarTrackerClass();
}

extern "C" STUNMANAGEDDLL_API void DisposeStarTrackerClass(StarTrackerClass* pObject)
{
    if (pObject != NULL)
    {
        delete pObject;
        pObject = NULL;
    }


}//End 'extern "C"' to prevent name mangling