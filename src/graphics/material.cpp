#include "material.h"

#include "application.h"

#include <istream>
#include <fstream>
#include <algorithm>

/////////////////////////// BASE MATERIALS ///////////////////////////

FlatMaterial::FlatMaterial(glm::vec4 color)
{
	this->color = color;
	this->shader = Shader::Get("res/shaders/basic.vs", "res/shaders/flat.fs");
}

FlatMaterial::~FlatMaterial() { }

void FlatMaterial::setUniforms(Camera* camera, glm::mat4 model)
{
	//upload node uniforms
	this->shader->setUniform("u_viewprojection", camera->viewprojection_matrix);
	this->shader->setUniform("u_camera_position", camera->eye);
	this->shader->setUniform("u_model", model);

	this->shader->setUniform("u_color", this->color);
}

void FlatMaterial::render(Mesh* mesh, glm::mat4 model, Camera* camera)
{
	if (mesh && this->shader) {
		// enable shader
		this->shader->enable();

		// upload uniforms
		setUniforms(camera, model);

		// do the draw call
		mesh->render(GL_TRIANGLES);

		this->shader->disable();
	}
}

void FlatMaterial::renderInMenu()
{
	ImGui::ColorEdit3("Color", (float*)&this->color);
}

WireframeMaterial::WireframeMaterial()
{
	this->color = glm::vec4(1.f);
	this->shader = Shader::Get("res/shaders/basic.vs", "res/shaders/flat.fs");
}

WireframeMaterial::~WireframeMaterial() { }

void WireframeMaterial::render(Mesh* mesh, glm::mat4 model, Camera* camera)
{
	if (this->shader && mesh)
	{
		glPolygonMode(GL_FRONT_AND_BACK, GL_LINE);
		glDisable(GL_CULL_FACE);

		//enable shader
		this->shader->enable();

		//upload material specific uniforms
		setUniforms(camera, model);

		//do the draw call
		mesh->render(GL_TRIANGLES);

		glEnable(GL_CULL_FACE);
		glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);
	}
}

StandardMaterial::StandardMaterial(glm::vec4 color)
{
	this->color = color;
	this->base_shader = Shader::Get("res/shaders/basic.vs", "res/shaders/basic.fs");
	this->normal_shader = Shader::Get("res/shaders/basic.vs", "res/shaders/normal.fs");
	this->shader = this->base_shader;
}

StandardMaterial::~StandardMaterial() { }

void StandardMaterial::setUniforms(Camera* camera, glm::mat4 model)
{
	//upload node uniforms
	this->shader->setUniform("u_viewprojection", camera->viewprojection_matrix);
	this->shader->setUniform("u_camera_position", camera->eye);
	this->shader->setUniform("u_model", model);

	this->shader->setUniform("u_color", this->color);

	if (this->texture) {
		this->shader->setUniform("u_texture", this->texture);
	}
}

void StandardMaterial::renderInMenu()
{
	if (ImGui::Checkbox("Show Normals", &this->show_normals)) {
		if (this->show_normals) {
			this->shader = this->normal_shader;
		}
		else {
			this->shader = this->base_shader;
		}
	}

	if (!this->show_normals) ImGui::ColorEdit3("Color", (float*)&this->color);
}

// Update Ambient/Background Light Here
void StandardMaterial::render(Mesh* mesh, glm::mat4 model, Camera* camera)
{
	bool first_pass = true;
	if (mesh && this->shader)
	{
		// enable shader
		this->shader->enable();

		// Multi pass render
		int num_lights = Application::instance->light_list.size();
		for (int nlight = -1; nlight < num_lights; nlight++)
		{
			if (nlight == -1) { nlight++; } // hotfix

			// upload uniforms
			setUniforms(camera, model);

			// upload light uniforms
			if (!first_pass) {
				glBlendFunc(GL_SRC_ALPHA, GL_ONE);
				glDepthFunc(GL_LEQUAL);
			}

			this->shader->setUniform("u_ambient_light", Application::instance->ambient_light * (float)first_pass);
			this->shader->setUniform("u_background_light", Application::instance->background_light);

			if (num_lights > 0) {
				Light* light = Application::instance->light_list[nlight];
				light->setUniforms(this->shader, model);
			}
			else {
				// Set some uniforms in case there is no light
				this->shader->setUniform("u_light_intensity", 1.f);
				this->shader->setUniform("u_light_shininess", 1.f);
				this->shader->setUniform("u_light_color", glm::vec4(0.f));
			}

			// do the draw call
			mesh->render(GL_TRIANGLES);

			first_pass = false;
		}

		// disable shader
		this->shader->disable();
	}
} 


//////////////////////////////////////////// NEW STUFF ////////////////////////////////////////////


VolumeMaterial::VolumeMaterial(glm::vec4 color)
{
	this->color = color;
	this->base_shader = Shader::Get("res/shaders/basic.vs", "res/shaders/basic.fs");
	this->normal_shader = Shader::Get("res/shaders/basic.vs", "res/shaders/normal.fs");
	this->abs_hom_shader = Shader::Get("res/shaders/basic.vs", "res/shaders/homogeneous.fs");
	this->abs_het_shader = Shader::Get("res/shaders/basic.vs", "res/shaders/heterogeneous.fs");
	this->emi_abs_shader = Shader::Get("res/shaders/basic.vs", "res/shaders/emissive_absorption.fs");

	//Choose initial shader
	
	switch (this->current_shader) {
	case 0:
		this->shader = this->base_shader;
		break;
	case 1:
		this->shader = this->normal_shader;
		break;
	case 2:
		this->shader = this->abs_hom_shader;
		break;
	case 3:
		this->shader = this->abs_het_shader;
		break;
	case 4:
		this->shader = this->emi_abs_shader;  // Ensure this shader is properly initialized
		break;
	}
}

void VolumeMaterial::setUniforms(Camera* camera, glm::mat4 model)
{
	//upload node uniforms
	this->shader->setUniform("u_viewprojection", camera->viewprojection_matrix);
	this->shader->setUniform("u_camera_position", camera->eye);
	this->shader->setUniform("u_model", model);

	if (this->texture) {
		this->shader->setUniform("u_texture", this->texture);
	}

	this->shader->setUniform("u_color", this->color);

	// ABS COEF
	this->shader->setUniform("u_abs_coef", this->absorption_coef);

	// STEP LENGTH
	this->shader->setUniform("u_step_length", this->step_length);

	// NOISE SCALE
	this->shader->setUniform("u_noise_scale", this->noise_scale);

	// NOISE DETAIL
	this->shader->setUniform("u_noise_detail", this->noise_detail);
	
}


void VolumeMaterial::renderInMenu()
{
	// Switch between Shaders

	if (ImGui::Combo("Shader Type", &this->current_shader, "Base\0Normal\0Homogeneous\0Heterogeneous\0Emission-Absorption")) {
	
		switch (this->current_shader) {
		case 0:
			this->shader = this->base_shader;
			break;
		case 1:
			this->shader = this->normal_shader;
			break;
		case 2:
			this->shader = this->abs_hom_shader;
			break;
		case 3:
			this->shader = this->abs_het_shader;
			break;
		case 4:
			this->shader = this->emi_abs_shader;  // Ensure this shader is properly initialized
			break;
		}
	}
	if (!this->show_normals) ImGui::ColorEdit3("Color", (float*)&this->color);

	ImGui::DragFloat("Absorption Coefficient", (float*)&this->absorption_coef, 0.025f, 0);

	ImGui::DragFloat("Step Length", (float*)&this->step_length, 0.0005f, 0.0001f);

	ImGui::DragFloat("Noise Scale", (float*)&this->noise_scale, 0.1f, 0.5);

	ImGui::DragFloat("Noise Detail", (float*)&this->noise_detail, 0.1f, 0.5);

}