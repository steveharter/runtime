cmake_minimum_required(VERSION 3.20)

project(Profiler)

set(SOURCES
    assemblyprofiler/assemblyprofiler.cpp
    classload/classload.cpp
    dynamicjitoptimization/dynamicjitoptimization.cpp
    eltprofiler/slowpatheltprofiler.cpp
    enumthreadsprofiler/enumthreadsprofiler.cpp
    eventpipeprofiler/eventpipereadingprofiler.cpp
    eventpipeprofiler/eventpipewritingprofiler.cpp
    eventpipeprofiler/eventpipemetadatareader.cpp
    gcallocateprofiler/gcallocateprofiler.cpp
    gcbasicprofiler/gcbasicprofiler.cpp
    gcheapenumerationprofiler/gcheapenumerationprofiler.cpp
    gcheapenumerationprofiler/gcheapenumerationprofiler.def
    gcprofiler/gcprofiler.cpp
    getappdomainstaticaddress/getappdomainstaticaddress.cpp
    handlesprofiler/handlesprofiler.cpp
    inlining/inlining.cpp
    metadatagetdispenser/metadatagetdispenser.cpp
    moduleload/moduleload.cpp
    multiple/multiple.cpp
    nongcheap/nongcheap.cpp
    nullprofiler/nullprofiler.cpp
    rejitprofiler/rejitprofiler.cpp
    rejitprofiler/ilrewriter.cpp
    rejitprofiler/sigparse.cpp
    releaseondetach/releaseondetach.cpp
    transitions/transitions.cpp
    profiler.def
    profiler.cpp
    classfactory.cpp
    dllmain.cpp
    guids.cpp)

include_directories(../../../coreclr/pal/prebuilt/inc)

add_compile_definitions(SOS_INCLUDE)

if(NOT WIN32)
    include_directories(../../../coreclr/pal/inc/rt ../../../coreclr/pal/inc ../../../coreclr/inc)
    if(CMAKE_CXX_COMPILER_ID MATCHES "Clang")
        add_compile_options(-Wno-null-arithmetic)
    else(CMAKE_CXX_COMPILER_ID MATCHES "Clang")
        add_compile_options(-Wno-conversion-null -Wno-pointer-arith)
    endif(CMAKE_CXX_COMPILER_ID MATCHES "Clang")
endif(NOT WIN32)

# add the executable
add_library(Profiler SHARED ${SOURCES})

if(WIN32)
	set(DEPENDENCIES Advapi32.lib oleaut32.lib Ole32.lib)
else(WIN32)
    set(DEPENDENCIES pthread ${CMAKE_DL_LIBS})
endif(WIN32)

target_link_libraries(Profiler PUBLIC ${DEPENDENCIES} ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install(TARGETS Profiler DESTINATION bin)

if(WIN32)
	install(FILES $<TARGET_PDB_FILE:${PROJECT_NAME}> DESTINATION bin OPTIONAL)
endif(WIN32)
