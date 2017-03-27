# CMake file for extract-frames

cmake_minimum_required(VERSION 3.0.0 FATAL_ERROR)

# the 'project' macro is used to name a project
project(extract-frames)

# adjust some global CMake configuration settings
set(CMAKE_VERBOSE_MAKEFILE TRUE)
set(CMAKE_COLOR_MAKEFILE TRUE)
set(CMAKE_SUPPRESS_REGENERATION TRUE)
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}/bin")

if(WIN32)
   if(MSVC)
      add_definitions(-DGLEW_STATIC)
      set(CMAKE_EXE_LINKER_FLAGS_RELEASE "/INCREMENTAL:NO ${CMAKE_EXE_LINKER_FLAGS}")
      set(CMAKE_EXE_LINKER_FLAGS_DEBUG "/DEBUG /NODEFAULTLIB:MSVCRT ${CMAKE_EXE_LINKER_FLAGS}")
   endif(MSVC)      
endif(WIN32)

if(CMAKE_CONFIGURATION_TYPES)
   set(CMAKE_INSTALL_PREFIX ./install)
   set(CMAKE_INSTALL_PREFIX "${CMAKE_INSTALL_PREFIX}" CACHE STRING
     "Reset the configurations to what we need"
     FORCE)
endif()

if(CMAKE_CONFIGURATION_TYPES)
    set(CMAKE_CONFIGURATION_TYPES Debug Release)
    set(CMAKE_CONFIGURATION_TYPES "${CMAKE_CONFIGURATION_TYPES}" CACHE STRING
      "Reset the configurations to what we need"
      FORCE)
endif()

# configure the Visual Studio user file
if(WIN32)
   if(MSVC)
	# find user and system name
	set(VC_USER_SYSTEM_NAME $ENV{USERDOMAIN} CACHE STRING SystemName)
	set(VC_USER_USER_NAME $ENV{USERNAME} CACHE STRING UserName)

	# configure the template file
	set(USER_FILE ${PROJECT_NAME}.vcxproj.user)
	set(OUTPUT_PATH ${CMAKE_CURRENT_BINARY_DIR}/${USER_FILE})

	# setup working directories in template file
	set(USERFILE_WORKING_DIRECTORY_DEBUG ${CMAKE_CURRENT_SOURCE_DIR}/bin/Debug)
	set(USERFILE_WORKING_DIRECTORY_RELEASE ${CMAKE_CURRENT_SOURCE_DIR}/bin/Release)
	set(USERFILE_ARGUMENTS_RELEASE "")
	set(USERFILE_ARGUMENTS_DEBUG "")
	configure_file(${CMAKE_HOME_DIRECTORY}/_CMAKE/CG-PROJECTS.vcxproj.usertemplate ${OUTPUT_PATH} @ONLY)
   endif(MSVC)
endif(WIN32)

# find sources
file(GLOB SRCS
    ./*.c
    ./*.cpp
)
source_group("src" FILES ${SRCS})

# find headers (let them show up in the IDEs)
file(GLOB HDRS
    ./*.h
    ./*.hpp
)
source_group("inc" FILES ${HDRS})

# find packages and libs
find_package( OpenCV REQUIRED)

# find framework (specific to mac)
if (APPLE)
	find_library(COCOA_LIBRARY Cocoa)
elseif()
	set(COCOA_LIBRARY "")
endif()

## setup package headers
include_directories( ${OpenCV_INCLUDE_DIRS} )

# setup debug/release libraries
set(LIBS_DEBUG)
foreach(lib; ${OpenCV_LIBS})
    list(APPEND LIBS_DEBUG debug ${lib})
endforeach()

set(LIBS_RELEASE)
foreach(lib; ${OpenCV_LIBS})
    list(APPEND LIBS_RELEASE optimized ${lib})
endforeach()

# define target dependencies and build instructions
set(EXECUTABLE_NAME ${PROJECT_NAME})
add_executable(${EXECUTABLE_NAME} ${SRCS} ${HDRS} ${GLSL})
# add framework (specific to mac)
target_link_libraries(${EXECUTABLE_NAME} ${LIBS_RELEASE} ${LIBS_DEBUG} ${COCOA_LIBRARY})

