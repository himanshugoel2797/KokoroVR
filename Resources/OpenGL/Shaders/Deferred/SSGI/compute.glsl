layout(local_size_x = 1) in;

uniform vec3 eyePos;
uniform vec3 eyeDir;

uniform vec3 lightPos;

layout(bindless_sampler, rgba16f) uniform sampler2D ColorMap;    //R:G:B:Roughness
layout(bindless_sampler, rgba32f) uniform sampler2D NormalMap;   //NX|NY|NZ:WX:WY:WZ
layout(bindless_sampler, rgba16f) uniform sampler2D SpecularMap; //SR:SG:SB
layout(bindless_image, rgba16f) uniform restrict writeonly image2D accumulator;
layout(bindless_sampler, rgba32f) uniform samplerCube positionBuf;

vec3 decode (float enc)
{
    return unpackSnorm4x8(floatBitsToUint(enc)).xyz;
}

void main(){
    //take a directional light as input for consideration, can do multiple passes
    //precompute occlusion distance field around camera every few frames, done by computing a low-res (128x128x6) position cubemap around the camera and applying camera movement + direction offsets
    //trace light visibility, determine shadowing and bounce lighting - configurable number of bounces
    //use a 4th channel to store compacted color information for reflections
    //will be very useful to generate simplified meshes for this pass
    //run at configurable full/half/quarter resolution

    //get current pixel index
    ivec2 pix_pos = ivec2(gl_GlobalInvocationID.xy);

    //get current pixel position
    vec4 pos = texelFetch(NormalMap, pix_pos, 0);
    vec4 color = texelFetch(ColorMap, pix_pos, 0);
    vec3 norm = decode(pos.x);
    float depth = length(pos.yzw - eyePos);
    //normalize(pos) provides the normal at which the corresponding cubemap position is
    //therefore, to trace, we simply step in the direction of the light and sample the cubemap

    vec4 r_pos = texture(positionBuf, (pos.yzw - eyePos));
    vec3 r_norm = decode(r_pos.w);

    //if(r_pos.xyz == pos)
    float nDl = 1;//min(max(dot(norm, normalize(lightPos - pos.yzw)), 0), 1);
    imageStore(accumulator, pix_pos, vec4(1));//vec4(r_norm * 0.5f + 0.5f, 1));//vec4(pos.xyz / 100.0f, 1));

    vec3 pix2light = normalize(lightPos - pos.yzw);
    //figure out if this pixel is in shadow by tracing a path in the lightDir
    for(int i = 1; i < 32; i++){
        vec3 rayPos = i * pix2light * 0.05f + pos.yzw;
        vec3 read_pos = texture(positionBuf, rayPos - eyePos).xyz;
        float read_depth = length(read_pos - eyePos);
        float ray_depth = length(rayPos - eyePos);

        if(read_depth + 0.000001f <= ray_depth){ //if read point is closer than the ray point, shadowed
            //shadowed
            imageStore(accumulator, pix_pos, vec4(0.2f));
            break;
        }
    }
}