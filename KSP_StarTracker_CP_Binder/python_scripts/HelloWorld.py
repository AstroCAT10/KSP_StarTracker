print('importing numpy...')
import numpy as np
print('done')
#import base64

def helloWorld1():
   print('Hello World from method1!!')

class PyClass:
   def __init__(self):
      pass
   
   def helloWorld(self):
      print('Hello World from method!!')

   def addInts(self, a, b):
      return np.int32(a + b)
   
   #def encode_decode(self):
   #   originalString = "Hello World!"
   #   print "Original string: " + originalString

   #   encodedString = base64.b64encode(originalString)
   #   print "Encoded string: " + encodedString

   #   decodedString = base64.b64decode(encodedString)
   #   print "Decoded string: " + decodedString