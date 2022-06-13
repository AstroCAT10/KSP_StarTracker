import sys
sys.path.insert(0, "C:\\Users\\benlu\\source\\repos\\TestExport1\\python_scripts\\tetra3")
import numpy as np
import cv2
import tetra3

from multiprocessing.pool import ThreadPool
#pool = ThreadPool(processes=1)


class StarTracker:
    def __init__(self):
        self.t3 = tetra3.Tetra3('C:\\Users\\benlu\\source\\repos\\c_sharp_py_test\\python_scripts\\tetra3\\catalogues\\tycho_tetra3.npz') #hip_tetra3.npz
        pass

    def SolveFromImage(self, img): #, img
        img = str(img)

        print("Python: image file is", img)
        print(cv2.__version__)

        cv2_image = cv2.imread(img) #/home/bpittelkau/bin/tetra3/test_data/2019-07-29T204726_Alt40_Azi-45_Try1.tiff
        print("Python: converting image to grayscale")
        gray_image = cv2.cvtColor(cv2_image, cv2.COLOR_BGR2GRAY)
        print(gray_image.shape)

        print("Python: solving...")
        # Solve for image, optionally passing known FOV estimate and error range
        result = self.t3.solve_from_image(gray_image) #, fov_estimate=20, fov_max_error=.5, **extract_dict
        quaternion = result['quaternion']
        print(quaternion)

        return quaternion


    def SolveFromArray(self, img_arr, len, w, h, num_layers = 3):
        #print(img_arr)
        #print(list(img_arr))

        img = np.array(list(img_arr), dtype='uint8')
        #img = np.asarray(img_arr, dtype='uint8')
        img = cv2.imdecode(img, cv2.IMREAD_COLOR)

        #np.savetxt('C:/Users/benlu/Desktop/Kerbal Space Program/KSP 1.12.3/GameData/HullCameraVDS/Plugins/PluginData/HullcamVDSContinued/test.txt', img, '%d')

        print(len,w,h,num_layers)
        
        #bound1 = len//3
        #bound2 = 2*len//3

        #r = img[0:bound1].reshape([h,w])
        #g = img[bound1:bound2].reshape([h,w])
        #b = img[bound2:len].reshape([h,w])

        #img = cv2.merge((b,g,r))

        #img.shape = (h, w, num_layers)
        #print(img.shape)

        img = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)

        result = self.t3.solve_from_image(img) #, fov_estimate=20, fov_max_error=.5, **extract_dict
        print(result)

        quaternion = result['quaternion']
        print(quaternion)

        if(quaternion[0] == None and quaternion[1] == None and quaternion[2] == None and quaternion[3] == None):
            quaternion = [0,0,0,0]

        #cv2.imshow('star image', img)

        #(this is necessary to avoid Python kernel form crashing)
        #cv2.waitKey(0) 
        #closing all open windows 
        #cv2.destroyAllWindows()

        return quaternion

    def SolveFromImageThread(self, img): #, img
        pool = ThreadPool(processes=1)

        print("Python: before creating thread")
        solver_thread = pool.apply_async(SolveFromImage2, (self.t3, img))
        print("Python: wait for the thread to finish")
        quaternion = solver_thread.get()
        pool.close()
        pool.join()
        print("Python: all done")

        return quaternion

    def SolveFromArrayThread(self, img_arr, len, w, h, num_layers = 3): #, img
        pool = ThreadPool(processes=1)

        print("Python: before creating thread")
        solver_thread = pool.apply_async(SolveFromArray2, (self.t3, img_arr, len, w, h, num_layers))
        print("Python: wait for the thread to finish")
        quaternion = solver_thread.get()
        pool.close()
        pool.join()
        print("Python: all done")

        return quaternion


def SolveFromImage2(t3, img):
    cv2_image = cv2.imread(img) #/home/bpittelkau/bin/tetra3/test_data/2019-07-29T204726_Alt40_Azi-45_Try1.tiff
    gray_image = cv2.cvtColor(cv2_image, cv2.COLOR_BGR2GRAY)
    print(gray_image.shape)
        

    # Solve for image, optionally passing known FOV estimate and error range
    result = t3.solve_from_image(gray_image) #, fov_estimate=20, fov_max_error=.5, **extract_dict
    print(result)

    quaternion = result['quaternion']
    print(quaternion)

    return quaternion


def SolveFromArray2(t3, img_arr, len, w, h, num_layers = 3):
    img = np.array(list(img_arr), dtype='uint8')
    img = cv2.imdecode(img, cv2.IMREAD_COLOR)

    print(len,w,h,num_layers) 

    img = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)

    result = t3.solve_from_image(img) #, fov_estimate=20, fov_max_error=.5, **extract_dict
    print(result)

    quaternion = result['quaternion']
    print(quaternion)

    if(quaternion[0] == None and quaternion[1] == None and quaternion[2] == None and quaternion[3] == None):
        quaternion = [0,0,0,0]

    #cv2.imshow('star image', img)

    #(this is necessary to avoid Python kernel form crashing)
    #cv2.waitKey(0) 
    #closing all open windows 
    #cv2.destroyAllWindows()

    return quaternion