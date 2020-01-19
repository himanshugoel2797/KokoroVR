layout(local_size_x = 64) in;

uniform vec3 eyePos;
uniform vec3 eyeDir;

uniform vec3 lightPos;
uniform float lightInten;

layout(bindless_sampler, rgba16f) uniform sampler2D ColorMap;    //R:G:B:Roughness
layout(bindless_sampler, rgba32f) uniform sampler2D NormalMap;   //NX|NY|NZ:WX:WY:WZ
layout(bindless_sampler, rgba16f) uniform sampler2D SpecularMap; //SR:SG:SB
layout(bindless_image, rgba16f) uniform restrict writeonly image2D accumulator;
layout(bindless_sampler, rgba32f) uniform samplerCube positionBuf;
layout(bindless_sampler, rgba16f) uniform samplerCube colorBuf;

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
    vec3 lightDir = lightPos - pos.yzw;
    float distSq = dot(lightDir, lightDir);
    lightDir = normalize(lightDir);
    vec3 norm = decode(pos.x);
    float nDl = min(max(dot(norm, lightDir), 0), 1);
    imageStore(accumulator, pix_pos, vec4(1));
    
    //Compute falloff
    float falloff = lightInten / distSq;
    if(falloff < 0.001f)return;
    if(nDl < 0.001f)return;
    
    vec3 rayPos = pos.yzw;
    vec3 rayStep = 0.075f * lightDir;

    //figure out if this pixel is in shadow by tracing a path in the lightDir
    for(int i = 0; i < 16; i++){
        rayPos += rayStep;
        vec3 eyeRel_rayPos = rayPos - eyePos;
        vec3 read_pos = texture(positionBuf, eyeRel_rayPos).xyz;
        vec3 eyeRel_readPos = read_pos - eyePos;

        float read_depth = dot(eyeRel_readPos, eyeRel_readPos);
        float ray_depth = dot(eyeRel_rayPos, eyeRel_rayPos);

        //Measure ray depth
        //imageStore(accumulator, pix_pos, vec4(i / 32.0f));
        if(read_depth + 0.000001f <= ray_depth){ //if read point is closer than the ray point, shadowed
            //shadowed
            imageStore(accumulator, pix_pos, vec4(0));
            break;
        }
    }
}