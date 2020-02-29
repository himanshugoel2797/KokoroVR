layout(location = 0) out vec2 uv;

vec2 positions[3] = vec2[](
    vec2(0.0, -0.5),
    vec2(0.5, 0.5),
    vec2(-0.5, 0.5)
);

void main() {
    float x = -1.0f + float((gl_VertexIndex & 1) << 2);
	float y = -1.0f + float((gl_VertexIndex & 2) << 1);
    
    gl_Position = vec4(x, y, 0.5f, 1.0f);
    uv = (vec2(x, y) + vec2(1)) * 0.5f;
}