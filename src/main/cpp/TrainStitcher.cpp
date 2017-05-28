#include <jni.h>
#include <vector>
#include <opencv2/stitching.hpp>

using namespace std;
using namespace cv;

extern "C"
{
  JNIEXPORT jint JNICALL Java_ch_sebastianhaeni_thermotrains_wrapper_Stitching_stitch
  (JNIEnv* env, jclass, jlongArray imageAddresses, jlong panorama)
  {
    vector<Mat> imgs;

    auto len = env->GetArrayLength(imageAddresses);
    auto addr = env->GetLongArrayElements(imageAddresses, nullptr);

    for (auto i = 0; i < len; i++)
    {
      auto& image = *reinterpret_cast<Mat*>(addr[i]);
      imgs.push_back(image);
    }

    auto& pano = *reinterpret_cast<Mat*>(panorama);

    auto stitcher = Stitcher::create(Stitcher::SCANS, true);
    
    //auto stitcher = Stitcher::createDefault(true);
    auto status = stitcher->stitch(imgs, pano);

    return status;
  }
}
