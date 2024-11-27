#version 450 core

in vec3 v_position;
in vec3 v_world_position;
in vec3 v_normal;

uniform vec3 u_camera_position;

uniform vec4 u_ambient_light;
uniform vec4 u_background_light;
uniform vec4 u_color;

uniform mat4 u_model;

uniform float u_abs_coef;
uniform float u_step_length;
uniform float u_noise_scale;
uniform float u_noise_detail;

uniform float u_scat_coef;

uniform sampler3D u_texture;  // 3D texture for density data
uniform int u_density_type;


uniform float u_light_intensity;
uniform vec4 u_light_color;
uniform vec3 u_light_direction;
uniform vec3 u_light_position;
uniform vec3 u_local_light_position;

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

#define MAX_OCTAVES 16

float fractal_noise( vec3 P, float detail )
{
    float fscale = 1.0;
    float amp = 1.0;
    float sum = 0.0;
    float octaves = clamp(detail, 0.0, 16.0);
    int n = int(octaves);

    for (int i = 0; i <= MAX_OCTAVES; i++) {
        if (i > n) continue;
        float t = noise(fscale * P);
        sum += t * amp;
        amp *= 0.5;
        fscale *= 2.0;
    }

    return sum;
}

float cnoise( vec3 P, float scale, float detail )
{
    P *= scale;
    return clamp(fractal_noise(P, detail), 0.0, 1.0);
}

// ---------------------------------------------------------------------------------------------------//

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

    vec3 position = rayOrigin + tNear * rayDir;
    vec4 sum = vec4(0.0);
    float thickness = 0.0;
    float density = 0.0;

    // Ray-marching loop for emission-absorption
    for (float t = tNear; t < tFar; t += u_step_length) {

        // Sample density from the 3D volume texture

        if (u_density_type == 0) {

            density = 1;
        }
        else if (u_density_type == 1) {

            density = cnoise(position, u_noise_scale, u_noise_detail);;
        } 
        else if (u_density_type == 2) {

            vec3 localPosition = (inverse(u_model) * vec4(position, 1.0)).xyz;

            vec3 texCoords = (localPosition + vec3(1.0)) * 0.5; // Map to [0, 1] range

            density = texture(u_texture, texCoords).x;
        }

        // ------------------ SCATTERING RAY MARCHING ---------------------

        vec3 rayOrigin2 = position;
        vec3 rayDir2 = normalize(u_light_position - position);

        // Intersect AABB
        tMin = (boxMin - rayOrigin2) / rayDir2;
        tMax = (boxMax - rayOrigin2) / rayDir2;
        t1 = min(tMin, tMax);
        t2 = max(tMin, tMax);
        float tFar2 = min(min(t2.x, t2.y), t2.z);

        vec3 position2 = rayOrigin2;
        float light_thickness = 0.0;
        float density2 = 0.0;

        
        // Second ray-marching
        for (float t = 0; t < tFar2; t += u_step_length) {

            // Sample density from the 3D volume texture

            if (u_density_type == 0) {

                density2 = 1;
            }
            else if (u_density_type == 1) {

                density2 = cnoise(position2, u_noise_scale, u_noise_detail);;
            } 
            else if (u_density_type == 2) {

                vec3 localPosition2 = (inverse(u_model) * vec4(position2, 1.0)).xyz;

                vec3 texCoords2 = (localPosition2 + vec3(1.0)) * 0.5; // Map to [0, 1] range

                density2 = texture(u_texture, texCoords2).x;
            }

            light_thickness += density2 * u_step_length;

            position2 += rayDir2 * u_step_length;

        }

        // ----------------------------------------------------------------
        
        float absorption = density * u_abs_coef;

        float scattering = exp(-light_thickness * 100) * u_scat_coef;

        float ext_coeff = absorption + scattering;

        //float transmittance = exp(-absorption * u_step_length);

        vec4 radiance = absorption * ext_coeff * u_color;

        vec4 scattering_light = scattering * u_light_color;

        //sum += transmittance * (radiance + scattering_light) * u_step_length;
        sum += (radiance + scattering_light) * u_step_length;

        // Advance position along the ray
        position += rayDir * u_step_length;

        // Accumulate optical thickness for background blending
        thickness += absorption * u_step_length;
    }

    // Final color calculation, combining accumulated radiance and background light
    FragColor = (1- exp(-thickness))*sum + u_background_light * exp(-thickness);
}