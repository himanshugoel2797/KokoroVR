//build mip pyramid with min() function
layout(local_size_x = 8, local_size_y = 8) in;

layout(std140, binding = 0) uniform GlobalParams_t {
	mat4 proj[EYECOUNT];
	mat4 view[EYECOUNT];
	mat4 vp[EYECOUNT];
	mat4 prev_view[EYECOUNT];
	mat4 prev_vp[EYECOUNT];
	uvec4 infoBindings[EYECOUNT];
	uvec4 depthBindings[EYECOUNT];
	vec4 prev_eyePos;
	vec4 prev_eyeUp;
	vec4 prev_eyeDir;
	vec4 eyePos;
	vec4 eyeUp;
	vec4 eyeDir;
} GlobalParams;

layout(std140, binding = 1) uniform HiZLayers_t {
    uvec4 sdpp;
    uvec4 mips[EYECOUNT][MIP_COUNT];
} HiZLayers;

uniform int eyeIdx;

void main(){
    //position = gl_GlobalInvocationID
    image2D dpthSrc = image2D(HiZLayers.mips[eyeIdx][HiZLayers.sdpp.x].xy);
    image2D dpthDst = image2D(HiZLayers.mips[eyeIdx][HiZLayers.sdpp.x + 1].xy);

    //compute min of src pixels
    ivec2 p = ivec2(gl_GlobalInvocationID.xy) * 2;
    vec2 f0 = imageLoad(dpthSrc, ivec2(p.x, p.y)).xy;
    vec2 f1 = imageLoad(dpthSrc, ivec2(p.x, p.y + 1)).xy;
    vec2 f2 = imageLoad(dpthSrc, ivec2(p.x + 1, p.y)).xy;
    vec2 f3 = imageLoad(dpthSrc, ivec2(p.x + 1, p.y + 1)).xy;

    float fDpth_min = f0.x;
    fDpth_min = min(f1.x, fDpth_min);
    fDpth_min = min(f2.x, fDpth_min);
    fDpth_min = min(f3.x, fDpth_min);

    float fDpth_max = f0.y;
    fDpth_max = max(f1.y, fDpth_max);
    fDpth_max = max(f2.y, fDpth_max);
    fDpth_max = max(f3.y, fDpth_max);

    //sample higher pixels and compute min from them
    imageStore(dpthDst, ivec2(gl_GlobalInvocationID.xy), vec4(fDpth_min, fDpth_max, 0, 1));
}