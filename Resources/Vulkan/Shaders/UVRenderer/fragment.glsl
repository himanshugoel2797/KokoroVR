layout(location = 0) in vec2 uv;

layout(location = 0) out vec4 outColor;
layout(location = 1) out vec4 outNorm;

void main() {
    outColor = vec4(uv, 1, 1);
    outNorm = vec4(1);
}