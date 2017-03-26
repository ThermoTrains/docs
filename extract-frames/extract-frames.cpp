#include "stdafx.h"
#include <opencv2/opencv.hpp>
#include <ostream>
#include <iostream>

using namespace std;
using namespace cv;

int main(int argc, char** argv)
{
  if (argc != 3)
  {
    cout << "Usage: extract-frames.exe <video> <frames>" << endl;
    return -1;
  }

  auto inputVideoFilename = argv[1];
  auto framesToExtract = atoi(argv[2]);

  VideoCapture cap(inputVideoFilename);

  if (!cap.isOpened())
  {
    cout << "Cannot open the video file" << endl;
    return -1;
  }

  auto frameCount = cap.get(CV_CAP_PROP_FRAME_COUNT);
  auto frameCounter = 0;

  for (auto i = 0; i < frameCount; i++)
  {
    Mat frame;
    auto success = cap.read(frame);
    if (!success)
    {
      cout << "Cannot read frame " << to_string(i) << endl;
      continue;
    }

    if (i == 0 || i % static_cast<int>(frameCount / framesToExtract) != 0)
    {
      // do not extract every frame, but once in a while so we have a fixed number of frames
      // not correlated to the frame count
      continue;
    }

    auto filename = "Frame " + to_string(++frameCounter) + ".jpg";

    imwrite(filename, frame);
    cout << "saved " << filename << endl;
  }
}
