#version 450 core

in vec3 v_position;
in vec3 v_world_position;
in vec3 v_normal;

uniform vec3 u_camera_position;
uniform vec4 u_color;
uniform vec4 u_ambient_light;
uniform vec3 u_light_position;
uniform vec4 u_light_color;
uniform float u_light_intensity;
uniform float u_light_shininess;
uniform vec4 u_background_light;
//uniform float u_step_length;        // Step length (controlled by GUI)


out vec4 FragColor;

// Noise functions
float hash1( float n )
{
    return fract( n*17.0*fract( n*0.3183099 ) );
}

float noise( vec3 x )
{
    vec3 p = floor(x);
    vec3 w = fract(x);
    
    vec3 u = w*w*w*(w*(w*6.0-15.0)+10.0);
    
    float n = p.x + 317.0*p.y + 157.0*p.z;
    
    float a = hash1(n+0.0);
    float b = hash1(n+1.0);
    float c = hash1(n+317.0);
    float d = hash1(n+318.0);
    float e = hash1(n+157.0);
    float f = hash1(n+158.0);
    float g = hash1(n+474.0);
    float h = hash1(n+475.0);

    float k0 =   a;
    float k1 =   b - a;
    float k2 =   c - a;
    float k3 =   e - a;
    float k4 =   a - b - c + d;
    float k5 =   a - c - e + g;
    float k6 =   a - b - e + f;
    float k7 = - a + b + c - d + e - f - g + h;

    return -1.0+2.0*(k0 + k1*u.x + k2*u.y + k3*u.z + k4*u.x*u.y + k5*u.y*u.z + k6*u.z*u.x + k7*u.x*u.y*u.z);
}

void main() {
    vec3 rayOrigin = u_camera_position;
    vec3 rayDir = normalize(v_world_position - u_camera_position);

    vec3 boxMin = vec3(-1);
    vec3 boxMax = vec3(1);

    // Intersect AABB
    vec3 tMin = (boxMin - rayOrigin) / rayDir;
    vec3 tMax = (boxMax - rayOrigin) / rayDir;
    vec3 t1 = min(tMin, tMax);
    vec3 t2 = max(tMin, tMax);
    float tNear = max(max(t1.x, t1.y), t1.z);
    float tFar = min(min(t2.x, t2.y), t2.z);

    // Initialize variables
    float thickness = 0.0;
    vec3 position = rayOrigin + tNear * rayDir;

	float u_step_length = 0.1;

    // Ray-marching loop
    for (float t = tNear; t < tFar; t += u_step_length) {
        float density = noise(position * 5);  // Sample 3D noise as density

        // Accumulate optical thickness
        thickness += density * u_step_length;

        // Advance position along the ray
        position += rayDir * u_step_length;
    }

    // Compute transmittance and final color
    float transmittance = exp(-thickness);
    FragColor = u_background_light * transmittance;
}
