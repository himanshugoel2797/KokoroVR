// Interpolated values from the vertex shaders
in vec2 UV;

// Ouput data
layout(location = 0) out vec4 f_color;

// Values that stay constant for the whole mesh.
layout(bindless_sampler) uniform sampler2D Accumulator;

void main(){
	vec3 color = texture(Accumulator, UV).rgb;
    f_color = vec4(color, 1);
return;
    color *= 1;  // Hardcoded Exposure Adjustment
    vec3 x = color / (1 + color);
    f_color.rgb = pow(x, vec3(1.0f / 2.2f));
	f_color.a = 1;
}