import cv2
import numpy as np
import scipy.io as scio

import sys
sys.path.insert(0, "/home/bpittelkau/bin/tetra3/")
import tetra3
#do: export PYTHONPATH=/home/bpittelkau/bin/tetra3/

cv2_image = cv2.imread('/home/bpittelkau/projects/star_matching/test_images/ksp_star_img3.png') #/home/bpittelkau/bin/tetra3/test_data/2019-07-29T204726_Alt40_Azi-45_Try1.tiff
gray_image = cv2.cvtColor(cv2_image, cv2.COLOR_BGR2GRAY)
print(gray_image.shape)

def solverInit():
    global t3, extract_dict
    # Create instance
    t3 = tetra3.Tetra3('/home/bpittelkau/bin/tetra3/catalogues/tycho_tetra3.npz') #default_database
    # Load a database
    #t3.load_database() #catalogues/tycho_tetra3.npz
    # Create dictionary with desired extraction settings
    extract_dict = {'min_sum': 250} #2000


imgs = tetra3.get_centroids_from_image(gray_image, sigma=3, image_th=None, crop=None, 
                                downsample=None, filtsize=25, bg_sub_mode='local_mean', 
                                sigma_mode='global_root_square', binary_open=True, centroid_window=None, 
                                max_area=None, min_area=None, max_sum=None, min_sum=3000, max_axis_ratio=None, 
                                max_returned=None, return_moments=False, return_images=True)

print(imgs[0])
for key, value in imgs[1].items() :
    print (key, value)

solverInit()

# Solve for image, optionally passing known FOV estimate and error range
result = t3.solve_from_image(gray_image) #, fov_estimate=20, fov_max_error=.5, **extract_dict
print(result)

cv2.namedWindow('Image', cv2.WINDOW_KEEPRATIO)
cv2.imshow('Image', gray_image) #imgs[1]['removed_background']
cv2.resizeWindow('Image', 700, 700)
if( cv2.waitKey(0) & 0xFF == ord('q') ):
   cv2.destroyAllWindows()

#sudo cp "/mnt/c/Users/benlu/Documents/Design Teams/RockSatX/Sensors/Star Tracker/test_images/gen_img2.jpg" /home/bpittelkau/projects/star_matching/test_images/

#sudo cp "/mnt/c/Users/benlu/Documents/Design Teams/RockSatX/Sensors/Star Tracker/TestData/OD90_Test2/dark_frameData.bin" /home/bpittelkau/projects/star_matching/dark_images/
