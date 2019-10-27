layout(local_size_x = 1, local_size_y = 1, local_size_z = 1) in;

layout(rgba16f, bindless_image) uniform restrict readonly image2D src;
layout(rgba16f, bindless_image) uniform restrict writeonly image2D dst;

const int offsets[] = {0, 1, 2, 3, 4};
const float weights[] = {0.2270270270, 0.1945945946, 0.1216216216,
                                  0.0540540541, 0.0162162162};

void main(){
//1,10,45,120,210,252,210,120,45,10,1
//1024
//Optimized weights
//45,120,210,252,210,120,45
//1024 - (1 + 10 + 10 + 1) = 1002
//Final weights
//0.04491017964,0.11976047904,0.209580838,0.251497006
//Offsets:
//-3, -2, -1, 0, 1, 2, 3
//New offsets:
//(3*0.04491017964 + 2*0.11976047904)/(0.04491017964 + 0.11976047904) = 2.2727273
//(2*0.11976047904 + 1*0.209580838)/(0.11976047904 + 0.209580838) = 1.3636364
//(1*0.209580838 + 0*0.251497006)/(0.209580838 + 0.251497006) = 0.4545455
//New Weights:
//0.04491017964 + 0.11976047904 = 0.1646707
//0.11976047904 + 0.209580838 = 0.32934132
//0.209580838 + 0.251497006 = 0.461077844


ivec2 coord = ivec2(gl_GlobalInvocationID.xy);

float res = 0;
vec4 src_pxl = imageLoad(src, coord);
res += src_pxl.g * weights[0];

for(int i = 1; i < 5; i++){
    res += imageLoad(src, ivec2(coord.x, coord.y + offsets[i])).g * weights[i];
    res += imageLoad(src, ivec2(coord.x, coord.y - offsets[i])).g * weights[i];
}

src_pxl.g = res;
imageStore(dst, coord, src_pxl);

}